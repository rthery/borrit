namespace BorritEditor.Database
{
    public interface IDatabaseSettings
    {
        bool HasBeenModified { get; }
		
        void OnGUI(string searchContext);
    }
}