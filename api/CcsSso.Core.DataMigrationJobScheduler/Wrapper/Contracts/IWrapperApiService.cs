﻿using CcsSso.Core.DataMigrationJobScheduler.Constants;

namespace CcsSso.Core.DataMigrationJobScheduler.Wrapper.Contracts
{
    public interface IWrapperApiService
    {
        Task<T> GetAsync<T>(WrapperApi wrapperApi, string? url, string errorMessage, bool cacheEnabledForRequest = false);

        Task<T> PostAsync<T>(WrapperApi wrapperApi, string? url, object requestData, string errorMessage);

        Task PutAsync(WrapperApi wrapperApi, string? url, object requestData, string errorMessage);
        Task<T> PutAsync<T>(WrapperApi wrapperApi, string? url, object requestData, string errorMessage);

        Task<bool> DeleteAsync(WrapperApi wrapperApi, string? url, string errorMessage);
        Task<T> DeleteAsync<T>(WrapperApi wrapperApi, string? url, string errorMessage);


    }
}
