namespace Graphode.Neo4j.FileTransfer
{
    public interface IFtpService
    {
        string GetHashOfCurrentFile(string ftpUrl, string filename);
        string GetMd5Sum(string str);
        void DeleteFile(string ftpUrl, string filename);
        void UploadFile(string ftpUrl, string filename, string fileText);
    }
}
