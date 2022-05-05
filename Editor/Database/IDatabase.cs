using System;
using System.Collections.Generic;

namespace BorritEditor.Database
{
	public interface IDatabase
	{
		bool IsInitialized { get; }
		IDatabaseSettings Settings { get; }
		
		event EventHandler<bool> OnInitialized;
		event EventHandler OnUpdated;

		void Initialize(string borrowerName, string projectName);
		void Reset();
		
		void BorrowAssets(string[] guids, string borrowerName);
		void ReturnAssets(string[] guids);
		void Refresh();
		DatabaseRow GetBorrowedAssetData(string guid);
		IReadOnlyList<DatabaseRow> GetBorrowedAssetsData();
		bool IsAssetBorrowed(string guid);
	}
}
