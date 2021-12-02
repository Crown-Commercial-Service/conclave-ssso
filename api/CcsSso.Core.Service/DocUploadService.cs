using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class DocUploadService : IDocUploadService
  {
    private readonly DocUploadConfig _docUploadConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataContext _dataContext;

    public DocUploadService(DocUploadConfig docUploadConfig, IHttpClientFactory httpClientFactory, IDataContext dataContext)
    {
      _docUploadConfig = docUploadConfig;
      _httpClientFactory = httpClientFactory;
      _dataContext = dataContext;
    }

    public async Task<DocUploadResponse> UploadFileAsync(string typeValidation, int sizeValidation, IFormFile file = null, string filePath = null)
    {
      if (file == null && filePath == null)
      {
        throw new CcsSsoException("ERROR_FILE_OR_FILEPATH_REQUIRED");
      }
      var client = _httpClientFactory.CreateClient("DocUploadApi");
      sizeValidation = sizeValidation == 0 ? _docUploadConfig.DefaultSizeValidationValue : sizeValidation;
      var formDataContent = new MultipartFormDataContent();
      formDataContent.Add(new StringContent(typeValidation, System.Text.Encoding.UTF8, "multipart/form-data"), "typeValidation[]");
      formDataContent.Add(new StringContent(sizeValidation.ToString(), System.Text.Encoding.UTF8, "multipart/form-data"), "sizeValidation");
      if (file != null)
      {
        //formDataContent.Add(file, "documentFile"); //TODO file
      }
      if (filePath != null)
      {
        formDataContent.Add(new StringContent($"{filePath}", System.Text.Encoding.UTF8, "multipart/form-data"), "documentFilePath");
      }
      var response = await client.PostAsync(string.Empty, formDataContent);

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var responseContent = JsonConvert.DeserializeObject<DocUploadResponse>(content);
        return responseContent;
      }
      else if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
      {
        throw new CcsSsoException("ERROR_WRONG_FILE_FORMAT");
      }
      else
      {
        throw new CcsSsoException("ERROR_UPLOADING_TO_DOC_UPLOAD");
      }
    }

    public async Task<DocUploadResponse> GetFileStatusAsync(string docId)
    {
      var client = _httpClientFactory.CreateClient("DocUploadApi");
      var response = await client.GetAsync($"{docId}");

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var responseContent = JsonConvert.DeserializeObject<DocUploadResponse>(content);
        return responseContent;
      }
      else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIVING_FROM_DOC_UPLOAD");
      }
    }


  }
}
