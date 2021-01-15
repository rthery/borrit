using System;

namespace BorritEditor.Database
{
	public interface IDatabase
	{
		bool IsInitialized { get; }
		IDatabaseSettings Settings { get; }
		
		event EventHandler<bool> OnInitialized;

		void Initialize(string borrowerName, string projectName);
		void Reset();
		
		void BorrowAssets(string[] guids, string borrowerName);
		void ReturnAssets(string[] guids);
		void Refresh();
		DatabaseRow GetBorrowedAssetData(string guid);
		bool IsAssetBorrowed(string guid);
	}
}
