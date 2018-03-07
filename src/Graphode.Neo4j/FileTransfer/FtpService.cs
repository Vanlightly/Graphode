using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Graphode.Neo4j.FileTransfer
{
    internal class FtpService : IFtpService
    {
        public string GetHashOfCurrentFile(string ftpUrl, string filename)
        {
            var currentText = string.Empty;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("anonymous", "graphode");
                    currentText = client.DownloadString(GetUrl(ftpUrl, filename));
                }
            }
            catch (WebException wex)
            {
                if (((FtpWebResponse)wex.Response).StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    return "";

                throw;
            }

            return GetMd5Sum(currentText);
        }

        public string GetMd5Sum(string str)
        {
            // First we need to convert the string into bytes, which
            // means using a text encoder.
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();

            // Create a buffer large enough to hold the string
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            // Now that we have a byte array we can ask the CSP to hash it
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);

            // Build the final string by converting each byte
            // into hex and appending it to a StringBuilder
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }

            // And return it
            return sb.ToString();
        }

        public void DeleteFile(string ftpUrl, string filename)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(GetUrl(ftpUrl, filename));

                //If you need to use network credentials
                request.Credentials = new NetworkCredential("anonymous", "graphode");
                //additionally, if you want to use the current user's network credentials, just use:
                //System.Net.CredentialCache.DefaultNetworkCredentials

                request.Method = WebRequestMethods.Ftp.DeleteFile;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
            }
            catch (WebException ex)
            {
                String status = ((FtpWebResponse)ex.Response).StatusDescription;
            }
        }

        public void UploadFile(string ftpUrl, string filename, string fileText)
        {
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential("anonymous", "graphode");
                client.UploadString(GetUrl(ftpUrl, filename), fileText);
            }
        }

        private string GetUrl(string ftpUrl, string filename)
        {
            // need a safe concat here
            return ftpUrl + filename;
        }
    }
}
