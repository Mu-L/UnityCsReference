// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreCache : ISerializationCallbackReceiver
    {
        private Dictionary<string, string> m_ETags = new Dictionary<string, string>();

        private Dictionary<string, long> m_Categories = new Dictionary<string, long>();

        private Dictionary<string, AssetStorePurchaseInfo> m_PurchaseInfos = new Dictionary<string, AssetStorePurchaseInfo>();

        private Dictionary<string, AssetStoreProductInfo> m_ProductInfos = new Dictionary<string, AssetStoreProductInfo>();

        private Dictionary<string, AssetStoreLocalInfo> m_LocalInfos = new Dictionary<string, AssetStoreLocalInfo>();

        private Texture2D m_MissingTexture;

        [SerializeField]
        private string[] m_SerializedKeys = new string[0];

        [SerializeField]
        private string[] m_SerializedETags = new string[0];

        [SerializeField]
        private string[] m_SerializedCategories = new string[0];

        [SerializeField]
        private long[] m_SerializedCategoryCounts = new long[0];

        [SerializeField]
        private AssetStorePurchaseInfo[] m_SerializedPurchaseInfos = new AssetStorePurchaseInfo[0];

        [SerializeField]
        private AssetStoreProductInfo[] m_SerializedProductInfos = new AssetStoreProductInfo[0];

        [SerializeField]
        private AssetStoreLocalInfo[] m_SerializedLocalInfos = new AssetStoreLocalInfo[0];

        public virtual event Action<IEnumerable<AssetStoreLocalInfo> /*addedOrUpdated*/, IEnumerable<AssetStoreLocalInfo> /*removed*/> onLocalInfosChanged;

        public virtual IEnumerable<AssetStoreLocalInfo> localInfos => m_LocalInfos.Values;

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(ApplicationProxy application, AssetStoreUtils assetStoreUtils, HttpClientFactory httpClientFactory, IOProxy systemIOProxy)
        {
            m_Application = application;
            m_HttpClientFactory = httpClientFactory;
            m_IOProxy = systemIOProxy;

            foreach (var productInfo in m_ProductInfos.Values)
                productInfo.ResolveDependencies(assetStoreUtils);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedKeys = m_ETags.Keys.ToArray();
            m_SerializedETags = m_ETags.Values.ToArray();

            m_SerializedCategories = m_Categories.Keys.ToArray();
            m_SerializedCategoryCounts = m_Categories.Values.ToArray();

            m_SerializedPurchaseInfos = m_PurchaseInfos.Values.ToArray();
            m_SerializedProductInfos = m_ProductInfos.Values.ToArray();
            m_SerializedLocalInfos = m_LocalInfos.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedKeys.Length; i++)
                m_ETags[m_SerializedKeys[i]] = m_SerializedETags[i];

            for (var i = 0; i < m_SerializedCategories.Length; i++)
                m_Categories[m_SerializedCategories[i]] = m_SerializedCategoryCounts[i];

            m_PurchaseInfos = m_SerializedPurchaseInfos.ToDictionary(info => info.productId.ToString(), info => info);
            m_ProductInfos = m_SerializedProductInfos.ToDictionary(info => info.id, info => info);
            m_LocalInfos = m_SerializedLocalInfos.ToDictionary(info => info.id, info => info);
        }

        public virtual string GetLastETag(string key)
        {
            return m_ETags.ContainsKey(key) ? m_ETags[key] : string.Empty;
        }

        public virtual void SetLastETag(string key, string etag)
        {
            m_ETags[key] = etag;
        }

        public virtual void SetCategory(string category, long count)
        {
            m_Categories[category] = count;
        }

        public virtual Texture2D LoadImage(long productId, string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var hash = Hash128.Compute(url);
            try
            {
                var path = m_IOProxy.PathsCombine(m_Application.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString(), hash.ToString());
                if (m_IOProxy.FileExists(path))
                {
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(m_IOProxy.FileReadAllBytes(path)))
                        return texture;
                }
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager] Cannot load image: {e.Message}");
            }

            return null;
        }

        public virtual void SaveImage(long productId, string url, Texture2D texture)
        {
            if (string.IsNullOrEmpty(url) || texture == null)
                return;

            try
            {
                var path = m_IOProxy.PathsCombine(m_Application.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString());
                if (!m_IOProxy.DirectoryExists(path))
                    m_IOProxy.CreateDirectory(path);

                var hash = Hash128.Compute(url);
                path = m_IOProxy.PathsCombine(path, hash.ToString());
                m_IOProxy.FileWriteAllBytes(path, texture.EncodeToJPG());
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager] Cannot save image: {e.Message}");
            }
        }

        public virtual void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null)
        {
            if (m_MissingTexture == null)
            {
                m_MissingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");
            }

            var texture = LoadImage(productID, url);
            if (texture != null)
            {
                doneCallbackAction?.Invoke(productID, texture);
                return;
            }

            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient(url);
            httpRequest.doneCallback = httpClient =>
            {
                if (httpClient.IsSuccess() && httpClient.texture != null)
                {
                    SaveImage(productID, url, httpClient.texture);
                    doneCallbackAction?.Invoke(productID, httpClient.texture);
                    return;
                }

                doneCallbackAction?.Invoke(productID, m_MissingTexture);
            };
            httpRequest.Begin();
        }

        public virtual void ClearCache()
        {
            m_ETags.Clear();
            m_Categories.Clear();

            m_PurchaseInfos.Clear();
            m_ProductInfos.Clear();
            m_LocalInfos.Clear();
            m_MissingTexture = null;
        }

        public virtual AssetStorePurchaseInfo GetPurchaseInfo(string productIdString)
        {
            return productIdString != null ? m_PurchaseInfos.Get(productIdString) : null;
        }

        public virtual AssetStoreProductInfo GetProductInfo(string productIdString)
        {
            return productIdString != null ? m_ProductInfos.Get(productIdString) : null;
        }

        public virtual AssetStoreLocalInfo GetLocalInfo(string productIdString)
        {
            return productIdString != null ? m_LocalInfos.Get(productIdString) : null;
        }

        public virtual void SetPurchaseInfo(AssetStorePurchaseInfo info)
        {
            m_PurchaseInfos[info.productId.ToString()] = info;
        }

        public virtual void SetProductInfo(AssetStoreProductInfo info)
        {
            m_ProductInfos[info.id] = info;
        }

        public virtual void SetLocalInfos(IEnumerable<AssetStoreLocalInfo> localInfos)
        {
            var oldLocalInfos = m_LocalInfos;
            m_LocalInfos = new Dictionary<string, AssetStoreLocalInfo>();
            foreach (var info in localInfos)
            {
                var id = info?.id;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (m_LocalInfos.TryGetValue(id, out var existingInfo))
                {
                    try
                    {
                        if (long.Parse(existingInfo.versionId) >= long.Parse(info.versionId))
                            continue;
                    }
                    catch (Exception)
                    {
                        var warningMessage = L10n.Tr("Multiple versions of the same package found on disk and we could not determine which one to take. Please remove one of the following files:\n");
                        Debug.LogWarning($"{warningMessage}{existingInfo.packagePath}\n{info.packagePath}");
                        continue;
                    }
                }
                m_LocalInfos[id] = info;
            }

            var addedOrUpdatedLocalInfos = new List<AssetStoreLocalInfo>();
            foreach (var info in m_LocalInfos.Values)
            {
                var oldInfo = oldLocalInfos.Get(info.id);
                if (oldInfo != null)
                    oldLocalInfos.Remove(info.id);

                var localInfoUpdated = oldInfo == null || oldInfo.versionId != info.versionId ||
                    oldInfo.versionString != info.versionString || oldInfo.packagePath != info.packagePath;
                if (localInfoUpdated)
                    addedOrUpdatedLocalInfos.Add(info);
                else
                    info.updateStatus = oldInfo.updateStatus;
            }
            if (addedOrUpdatedLocalInfos.Any() || oldLocalInfos.Any())
                onLocalInfosChanged?.Invoke(addedOrUpdatedLocalInfos, oldLocalInfos.Values);
        }

        public virtual void RemoveProductInfo(string productIdString)
        {
            m_ProductInfos.Remove(productIdString);
        }
    }
}
