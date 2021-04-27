using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace BorritEditor.Database.GoogleSheets
{
	public class GoogleSheetsDatabase : IDatabase
	{
		private const string ApplicationName = "Borrit";
		private const string Dimension = "ROWS";
		
		private SheetsService _service;
		private List<IList<object>> _data;
		private string _range;
		
		public bool IsInitialized => _service != null;
		public IDatabaseSettings Settings { get; private set; }
		public bool HasAuthToken => AuthTokenUserFilePath != string.Empty;

		public string AuthTokenFolder
		{
			get
			{
				PackageInfo packageInfo = PackageInfo.FindForAssembly(typeof(BorritSettings).Assembly);
				string tokenFolder = PackageSettingsRepository.GetSettingsPath(packageInfo.name, "Tokens").Replace(".json", string.Empty);
				return tokenFolder;
			}
		}
		
		public string AuthTokenUserFilePath
		{
			get
			{
				string userName = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
				string tokenPath = Path.Combine(AuthTokenFolder, $"Google.Apis.Auth.OAuth2.Responses.TokenResponse-{userName}");
				if (File.Exists(tokenPath))
					return tokenPath;

				return string.Empty;
			}
		}

		public event EventHandler<bool> OnInitialized;
		public event EventHandler OnUpdated;

		public void Initialize(string borrowerName, string projectName)
		{
			Settings = new GoogleSheetsSettings();

			if (string.IsNullOrEmpty(AuthTokenUserFilePath))
				return;
			
			_range = $"'{projectName}'";

			UserCredential userCredential = CreateAuthToken().Result;
			BaseClientService.Initializer initializer = new BaseClientService.Initializer();
			initializer.HttpClientInitializer = userCredential;
			initializer.ApplicationName = ApplicationName;
			_service = new SheetsService(initializer);
			
			_data = new List<IList<object>>(64);
			
			OnInitialized?.Invoke(this, true);
		}

		public void Reset()
		{
			if (IsAuthenticating)
				_authCancellationTokenSource.Cancel();
			
			if (IsInitialized == false)
				return;
			
			Settings = null;
			
			_service.Dispose();
			_service = null;
			_data = null;
		}

		public void BorrowAssets(string[] guids, string borrowerName)
		{
			if (IsInitialized == false)
				return;
			
			string spreadsheetId = BorritSettings.Instance.Get<string>(GoogleSheetsSettings.Keys.SpreadsheetId);
			if (string.IsNullOrEmpty(spreadsheetId))
				return;

			if (guids == null)
				throw new ArgumentNullException(nameof(guids));
			List<IList<object>> dataList = new List<IList<object>>(guids.Length);
			for (int i = 0; i < guids.Length; i++)
			{
				List<object> item = new List<object>();
				item.Add(guids[i]);
				item.Add(borrowerName);
				item.Add(DateTime.UtcNow.ToBinary().ToString());
				
				dataList.Add(item);
			}
			
			ValueRange valueDataRange = new ValueRange();
			valueDataRange.Values = dataList;
			valueDataRange.MajorDimension = Dimension;
			
			SpreadsheetsResource.ValuesResource.AppendRequest request = _service.Spreadsheets.Values.Append(valueDataRange, spreadsheetId, _range);
			request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
			request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
			request.Execute();
			
			_data.AddRange(dataList);
			
			OnUpdated?.Invoke(this, EventArgs.Empty);
		}
		
		public void ReturnAssets(string[] guids)
		{
			if (IsInitialized == false)
				return;
			
			string spreadsheetId = BorritSettings.Instance.Get<string>(GoogleSheetsSettings.Keys.SpreadsheetId);
			if (string.IsNullOrEmpty(spreadsheetId))
				return;
			
			if (guids == null)
				throw new ArgumentNullException(nameof(guids));
			
			List<IList<object>> dataList = new List<IList<object>>(_data);
			List<Request> requests = new List<Request>(guids.Length);
			foreach (string guid in guids)
			{
				for (int i = dataList.Count - 1; i >= 0; i--)
				{
					if (dataList[i] == null)
						continue;

					if (dataList[i][DatabaseColumn.BorrowedAssetIndex] as string == guid)
					{
						dataList.RemoveAt(i);
						
						Request requestBody = new Request();
						requestBody.DeleteDimension = new DeleteDimensionRequest();
						requestBody.DeleteDimension.Range = new DimensionRange();
						requestBody.DeleteDimension.Range.EndIndex = i + 1;
						requestBody.DeleteDimension.Range.StartIndex = i;
						requestBody.DeleteDimension.Range.Dimension = Dimension;
						requestBody.DeleteDimension.Range.SheetId = 0;
						requests.Add(requestBody);
					}
				}
			}
			
			BatchUpdateSpreadsheetRequest batchRequestBody = new BatchUpdateSpreadsheetRequest();
			batchRequestBody.Requests = requests;
			
			SpreadsheetsResource.BatchUpdateRequest request = new SpreadsheetsResource.BatchUpdateRequest(_service, batchRequestBody, spreadsheetId);
			request.Execute();
			
			_data = dataList;
			
			OnUpdated?.Invoke(this, EventArgs.Empty);
		}

		public void Refresh()
		{
			if (IsInitialized == false)
				return;
			
			string spreadsheetId = BorritSettings.Instance.Get<string>(GoogleSheetsSettings.Keys.SpreadsheetId);
			if (string.IsNullOrEmpty(spreadsheetId))
				return;
			
			SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(spreadsheetId, _range);
			ValueRange response = request.Execute();
			
			_data = response.Values as List<IList<object>>;
			if (_data == null)
				_data = new List<IList<object>>();
		}
		
		public DatabaseRow GetBorrowedAssetData(string guid)
		{
			if (IsInitialized == false)
				return DatabaseRow.Empty;
			
			if (string.IsNullOrEmpty(guid))
				throw new ArgumentNullException(nameof(guid));
			
			// TODO Also cache rows in a dictionary with their guid as key to reduce iterations
			foreach (IList<object> row in _data)
			{
				bool rowIsWellFormatted = row != null && row.Count == DatabaseColumn.Count;
				if (rowIsWellFormatted && row[DatabaseColumn.BorrowedAssetIndex] as string == guid)
					return new DatabaseRow(row);
			}

			return DatabaseRow.Empty;
		}

		public bool IsAssetBorrowed(string guid)
		{
			string spreadsheetId = BorritSettings.Instance.Get<string>(GoogleSheetsSettings.Keys.SpreadsheetId);
			if (string.IsNullOrEmpty(spreadsheetId))
				return true;
			
			return GetBorrowedAssetData(guid).IsEmpty == false;
		}

		private CancellationTokenSource _authCancellationTokenSource;
		public bool IsAuthenticating => _authCancellationTokenSource != null;
		public async Task<UserCredential> CreateAuthToken()
		{
			string borrowerName = BorritSettings.Instance.Get<string>(BorritSettings.Keys.Username, SettingsScope.User);
			string credentialsPath = BorritSettings.Instance.Get<string>(GoogleSheetsSettings.Keys.Credentials);
			UserCredential userCredential = null;
			
			using (FileStream stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
			{
				ClientSecrets secrets;
				try
				{
					secrets = GoogleClientSecrets.Load(stream).Secrets;
				}
				catch (Exception)
				{
					BorritSettings.Instance.Set(GoogleSheetsSettings.Keys.Credentials, string.Empty);
					BorritSettings.Instance.Save();
					Debug.LogError($"{credentialsPath} is not a valid credentials JSON file!");
					return null;
				}

				_authCancellationTokenSource = new CancellationTokenSource();

				Task<UserCredential> longRunningTask = GoogleWebAuthorizationBroker.AuthorizeAsync(
					secrets,
					new[] {SheetsService.Scope.Spreadsheets},
					borrowerName,
					_authCancellationTokenSource.Token,
					new FileDataStore(AuthTokenFolder, true));
				
				try
				{
					userCredential = await longRunningTask;
				}
				catch (Exception)
				{
					if (_authCancellationTokenSource.IsCancellationRequested == false)
						throw;
				}
			}

			if (_authCancellationTokenSource != null)
			{
				_authCancellationTokenSource.Dispose();
				_authCancellationTokenSource = null;
			}

			return userCredential;
		}

		public void CancelAuthTokenCreation()
		{
			_authCancellationTokenSource.Cancel();
		}
	}
}
