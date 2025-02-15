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
    internal class UpmCache : ISerializationCallbackReceiver
    {
        private Dictionary<string, PackageInfo> m_SearchPackageInfos = new Dictionary<string, PackageInfo>();
        private Dictionary<string, PackageInfo> m_InstalledPackageInfos = new Dictionary<string, PackageInfo>();
        private Dictionary<string, PackageInfo> m_ProductPackageInfos = new Dictionary<string, PackageInfo>();

        private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfo = new Dictionary<string, Dictionary<string, PackageInfo>>();

        // the mapping between package name (key) to asset store product id (value)
        private Dictionary<string, string> m_ProductIdMap = new Dictionary<string, string>();

        private readonly Dictionary<string, Dictionary<string, object>> m_ParsedUpmReserved = new Dictionary<string, Dictionary<string, object>>();

        [SerializeField]
        private long m_SearchPackageInfosTimestamp = 0;

        // arrays created to help serialize dictionaries
        [SerializeField]
        private PackageInfo[] m_SerializedInstalledPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedSearchPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedProductPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedExtraPackageInfos;
        [SerializeField]
        private string[] m_SerializedProductIdMapKeys;
        [SerializeField]
        private string[] m_SerializedProductIdMapValues;

        public virtual event Action<IEnumerable<PackageInfo>> onPackageInfosUpdated;
        public virtual event Action<string> onVerifiedGitPackageUpToDate;

        public virtual IEnumerable<PackageInfo> searchPackageInfos => m_SearchPackageInfos.Values;
        public virtual IEnumerable<PackageInfo> installedPackageInfos => m_InstalledPackageInfos.Values;
        public virtual IEnumerable<PackageInfo> productPackageInfos => m_ProductPackageInfos.Values;

        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(PackageManagerPrefs packageManagerPrefs)
        {
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        private static List<PackageInfo> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
        {
            return newInfos.Values.Where(p => !oldInfos.TryGetValue(p.name, out var info) || IsDifferent(info, p))
                .Concat(oldInfos.Values.Where(p => !newInfos.ContainsKey(p.name))).ToList();
        }

        // For BuiltIn and Registry packages, we want to only compare a subset of PackageInfo attributes,
        // as most attributes never change if their PackageId is the same. For other types of packages, always consider them different
        private static bool IsDifferent(PackageInfo p1, PackageInfo p2)
        {
            if (p1.packageId != p2.packageId ||
                p1.isDirectDependency != p2.isDirectDependency ||
                p1.version != p2.version ||
                p1.source != p2.source ||
                p1.resolvedPath != p2.resolvedPath ||
                p1.entitlements.isAllowed != p2.entitlements.isAllowed ||
                p1.entitlements.licenseType != p2.entitlements.licenseType ||
                p1.registry?.id != p2.registry?.id ||
                p1.registry?.name != p2.registry?.name ||
                p1.registry?.url != p2.registry?.url ||
                p1.registry?.isDefault != p2.registry?.isDefault ||
                p1.versions.verified != p2.versions.verified ||
                p1.versions.compatible.Length != p2.versions.compatible.Length || !p1.versions.compatible.SequenceEqual(p2.versions.compatible) ||
                p1.versions.all.Length != p2.versions.all.Length || !p1.versions.all.SequenceEqual(p2.versions.all) ||
                p1.errors.Length != p2.errors.Length || !p1.errors.SequenceEqual(p2.errors) ||
                p1.dependencies.Length != p2.dependencies.Length || !p1.dependencies.SequenceEqual(p2.dependencies) ||
                p1.resolvedDependencies.Length != p2.resolvedDependencies.Length || !p1.resolvedDependencies.SequenceEqual(p2.resolvedDependencies) ||
                p1.projectDependenciesEntry != p2.projectDependenciesEntry ||
                p1.signature.status != p2.signature.status ||
                p1.documentationUrl != p2.documentationUrl ||
                p1.changelogUrl != p2.changelogUrl ||
                p1.licensesUrl != p2.licensesUrl)
                return true;

            if (p1.source == PackageSource.BuiltIn || p1.source == PackageSource.Registry)
                return false;

            if (p1.source == PackageSource.Git)
                return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

            return true;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedInstalledPackageInfos = m_InstalledPackageInfos.Values.ToArray();
            m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
            m_SerializedProductPackageInfos = m_ProductPackageInfos.Values.ToArray();
            m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();
            m_SerializedProductIdMapKeys = m_ProductIdMap.Keys.ToArray();
            m_SerializedProductIdMapValues = m_ProductIdMap.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedInstalledPackageInfos)
                m_InstalledPackageInfos[p.name] = p;

            foreach (var p in m_SerializedSearchPackageInfos)
                m_SearchPackageInfos[p.name] = p;

            m_ProductPackageInfos = m_SerializedProductPackageInfos.ToDictionary(p => p.name, p => p);

            foreach (var p in m_SerializedExtraPackageInfos)
                AddExtraPackageInfo(p);

            for (var i = 0; i < m_SerializedProductIdMapKeys.Length; i++)
                m_ProductIdMap[m_SerializedProductIdMapKeys[i]] = m_SerializedProductIdMapValues[i];
        }

        public virtual void AddExtraPackageInfo(PackageInfo packageInfo)
        {
            Dictionary<string, PackageInfo> dict;
            if (!m_ExtraPackageInfo.TryGetValue(packageInfo.name, out dict))
            {
                dict = new Dictionary<string, PackageInfo>();
                m_ExtraPackageInfo[packageInfo.name] = dict;
            }
            dict[packageInfo.version] = packageInfo;
        }

        public virtual Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName) => m_ExtraPackageInfo.Get(packageName);

        public virtual void RemoveInstalledPackageInfo(string packageName)
        {
            var oldInfo = m_InstalledPackageInfos.Get(packageName);
            if (oldInfo == null)
                return;

            m_InstalledPackageInfos.Remove(packageName);
            TriggerOnPackageInfosUpdated(new PackageInfo[] { oldInfo });
        }

        public virtual bool IsPackageInstalled(string packageName) => m_InstalledPackageInfos.ContainsKey(packageName);

        public virtual PackageInfo GetInstalledPackageInfo(string packageName) => m_InstalledPackageInfos.Get(packageName);

        public virtual PackageInfo GetInstalledPackageInfoById(string packageId)
        {
            var idSplit = packageId?.Split(new[] { '@' }, 2);
            return idSplit?.Length == 2 ? GetInstalledPackageInfo(idSplit[0]) : null;
        }

        public virtual void SetInstalledPackageInfo(PackageInfo info, bool isSpecialInstallation)
        {
            var oldInfo = m_InstalledPackageInfos.Get(info.name);
            m_InstalledPackageInfos[info.name] = info;
            if (isSpecialInstallation || oldInfo == null || IsDifferent(oldInfo, info))
                TriggerOnPackageInfosUpdated(new PackageInfo[] { info });

            // if Git install and oldInfo is same as new info, means no update was found
            else if (oldInfo.source == PackageSource.Git && !IsDifferent(oldInfo, info))
            {
                onVerifiedGitPackageUpToDate.Invoke(oldInfo.name);
            }
        }

        public virtual void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_InstalledPackageInfos;
            m_InstalledPackageInfos = newPackageInfos;

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);

            // This is to fix the issue where refresh in `In Project` doesn't show new versions from the registry
            // The cause of that issue is that when we create a UpmPackage, we take the versions from searchInfo
            // and augment with the installed version but the versions in searchInfo could be outdated sometimes
            // Since we have no easy way to compare two package info and know which one is newer with the current implementation,
            // we want to keep what's stored in the searchPackageInfos as up to date as possible,
            if (timestamp > m_SearchPackageInfosTimestamp)
                foreach (var newInfo in updatedInfos.Where(info => m_SearchPackageInfos.ContainsKey(info.name) && info.errors.Length == 0))
                    m_SearchPackageInfos[newInfo.name] = newInfo;

            if (updatedInfos.Any())
                TriggerOnPackageInfosUpdated(updatedInfos);
        }

        public virtual PackageInfo GetSearchPackageInfo(string packageName) => m_SearchPackageInfos.Get(packageName);

        public virtual void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_SearchPackageInfos;
            m_SearchPackageInfos = newPackageInfos;

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);
            if (updatedInfos.Any())
                TriggerOnPackageInfosUpdated(updatedInfos);

            m_SearchPackageInfosTimestamp = timestamp;
        }

        public virtual PackageInfo GetProductPackageInfo(string packageName) => m_ProductPackageInfos.Get(packageName);
        public virtual void SetProductPackageInfo(string productId, PackageInfo info)
        {
            m_ProductIdMap[info.name] = productId;
            var oldInfo = m_ProductPackageInfos.Get(info.name);
            m_ProductPackageInfos[info.name] = info;
            if (oldInfo == null || IsDifferent(oldInfo, info))
                TriggerOnPackageInfosUpdated(new PackageInfo[] { info });
        }

        private void TriggerOnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
        {
            foreach (var info in packageInfos)
                m_ParsedUpmReserved.Remove(info.packageId);
            onPackageInfosUpdated?.Invoke(packageInfos);
        }

        public virtual Dictionary<string, object> ParseUpmReserved(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.upmReserved))
                return null;

            if (!m_ParsedUpmReserved.TryGetValue(packageInfo.packageId, out var result))
            {
                result = Json.Deserialize(packageInfo.upmReserved) as Dictionary<string, object>;
                m_ParsedUpmReserved[packageInfo.packageId] = result;
            }
            return result;
        }

        public virtual string GetProductId(string packageName) => m_ProductIdMap.Get(packageName);

        public virtual void ClearCache()
        {
            m_InstalledPackageInfos.Clear();
            m_SearchPackageInfos.Clear();
            m_ExtraPackageInfo.Clear();
            m_ProductIdMap.Clear();

            m_SerializedInstalledPackageInfos = new PackageInfo[0];
            m_SerializedSearchPackageInfos = new PackageInfo[0];
            m_SerializedExtraPackageInfos = new PackageInfo[0];

            ClearProductCache();
        }

        public virtual void ClearProductCache()
        {
            m_ProductPackageInfos.Clear();
            m_ProductIdMap.Clear();

            m_SerializedProductPackageInfos = new PackageInfo[0];
            m_SerializedProductIdMapKeys = new string[0];
            m_SerializedProductIdMapValues = new string[0];
        }
    }
}
