using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;


namespace WikiScraper
{
    /// <summary>
    /// interface for loading/uploading monthly text files
    /// </summary>
    interface IDBClient
    {
        Task<HttpStatusCode>                            Upload(string _YYYYMM, string _content);
        Task<(HttpStatusCode result, string content)>   Load(string _YYYYMM);
    }

    /// <summary>
    /// github uploader based on Octokit
    /// </summary>
    internal class GithubUploader : IDBClient
    {
        GitHubClient client;

        string owner;
        string repo;
        string branch;
        string targetFileHeader; //todo move to config

        public GithubUploader(string _productHeader, string _gitToken, 
            string _owner, string _repo, string _branch, string _filePath)
        {
            owner = _owner;
            repo = _repo;
            branch = _branch;
            client = new GitHubClient(new ProductHeaderValue(_productHeader))
            {
                Credentials = new Credentials(_gitToken)
            };
            targetFileHeader = _filePath;
        }

        public async Task<HttpStatusCode> Upload(string _YYYYMM, string _content)
        {
            
            try
            {
                // try to get the file (and with the file the last commit sha)
                var existingFile = await client.Repository.Content.GetAllContentsByRef(
                    owner, repo, targetFileHeader + _YYYYMM + ".json", branch);

                // update the file
                var updateChangeSet = await client.Repository.Content.UpdateFile(
                    owner, repo, targetFileHeader + _YYYYMM + ".json",
                   new UpdateFileRequest("File upload: " + DateTime.UtcNow, _content, existingFile.First().Sha, branch));
                return HttpStatusCode.OK;
            }
            catch (Octokit.NotFoundException)
            {
                // if file is not found, create it
                
                var createChangeSet = await client.Repository.Content.CreateFile(
                    owner, repo, targetFileHeader + _YYYYMM +".json", 
                    new CreateFileRequest("File upload: " + DateTime.UtcNow, _content, branch));
                return HttpStatusCode.OK;
            }
        }

        public async Task<(HttpStatusCode result, string content)> Load(string _YYYYMM)
        {
            try
            {
                // try to get the file (and with the file the last commit sha)
                var existingFile = await client.Repository.Content.GetAllContentsByRef(
                    owner, repo, targetFileHeader + _YYYYMM + ".json", branch);

                return (HttpStatusCode.OK, existingFile.First().Content);
            }
            catch (Octokit.NotFoundException)
            {
                return (HttpStatusCode.NotFound, "");
            }
        }
    }
}
