namespace QrF.Core.Config
{
    public interface IConfigService
    {
        string GetConfig(string name);
        void SaveConfig(string name, string content);
        string GetFilePath(string name);
    }
}
