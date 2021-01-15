using System;

namespace BorritEditor.Database.LocalFile
{
    // TODO Not implemented, this will support a csv file that the team can share through a third party file sync service
    public class LocalFileDatabase : IDatabase
    {
        public bool IsInitialized { get; private set; }
        
        public IDatabaseSettings Settings { get; private set; }

        public void Initialize(string borrowerName, string projectName)
        {
            Settings = new LocalFileSettings();

            IsInitialized = true;
            
            OnInitialized?.Invoke(this, true);
        }

        public void Reset()
        {
            if (IsInitialized == false)
                return;
            
            Settings = null;
            IsInitialized = false;
        }

        public void BorrowAssets(string[] guids, string borrowerName)
        {
            throw new NotImplementedException();
        }

        public void ReturnAssets(string[] guids)
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            
        }

        public DatabaseRow GetBorrowedAssetData(string guid)
        {
            return DatabaseRow.Empty;
        }

        public bool IsAssetBorrowed(string guid)
        {
            return false;
        }

        public event EventHandler<bool> OnInitialized;
    }
}