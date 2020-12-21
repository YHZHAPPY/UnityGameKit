using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class AddressableManager : Singleton<AddressableManager>
{
    /// <remarks>
    /// Addressable全部是异步加载
    /// 需要缓存加载信息，避免重复加载
    /// </remarks>
    private Dictionary<string, AsyncOperationHandle> caches = new Dictionary<string, AsyncOperationHandle>();
    /// <remarks>初始化启动定时器，定时清理无用的缓存</remarks>
    private bool isInit=false;
    /// <remarks>清理时间间隔</remarks>
    private int timeSpan=600;

    /// <summary>
    /// 清理参数
    /// </summary>
    public const float Exceed = 1.2f;

    public event System.Action ClearCaches;

    private AddressablePool<Transform> _pool;
    public AddressablePool<Transform> pool
    {
        get 
        {
            if (_pool == null)
            {
                GameObject poolNode = new GameObject("Addressable Pool");
                poolNode.transform.localPosition = Vector3.zero;
                poolNode.transform.localScale = Vector3.one;
                poolNode.transform.localRotation = Quaternion.identity;
                GameObject.DontDestroyOnLoad(poolNode);
                _pool = new AddressablePool<Transform>(poolNode.transform);
            }
            return _pool;
        }
    }

    public override bool Init()
    {
        if (isInit)
            return true;
        isInit = true;
        Scheduler.Instance.Repeat(timeSpan, () => {
            if (this.ClearCaches != null)
            {
                this.ClearCaches.Invoke();
            }
        });
        return true;
    }

    /// <remarks>资源单个加载</remarks>
    /// <param name="address">资源地址</param>
    public void LoadAsset<T>(string address, System.Action<T> onComplete, System.Action onFailed = null, bool autoUnload = false) where T : UnityEngine.Object
    {
        if (caches.ContainsKey(address))
        {
            var handle = this.caches[address];
            if (handle.IsDone)
            {
                if (onComplete != null)
                {
                    onComplete(caches[address].Result as T);
                }
            }
            else
            {
                handle.Completed += (result) => {
                    if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        var obj = result.Result as T;
                        if (onComplete != null)
                        {
                            onComplete(obj);
                        }
                        if (autoUnload)
                            UnLoadAsset(address);
                    }
                    else
                    {
                        if (onFailed != null)
                        {
                            onFailed();
                        }
                        Helper.LogError("Load " + address + " failed!");
                    }
                };
            }

        }
        else
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.Completed += (result) =>
            {
                if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    var obj = result.Result as T;
                    if (onComplete != null)
                    {
                        onComplete(obj);
                    }
                    if (autoUnload)
                        UnLoadAsset(address);
                }
                else
                {
                    if (onFailed != null)
                    {
                        onFailed();
                    }
                    Helper.LogError("Load " + address + " failed!");
                }
            };
            addCaches(address, handle);
        }
    }

    /// <remarks>资源多个资源加载</remarks>
    /// <param name="address">资源地址标签</param>
    public AsyncOperationHandle LoadAssets<T>(string address, System.Action<List<T>> onComplete, System.Action onFailed = null, bool autoUnload = false) where T : UnityEngine.Object
    {
        if (this.caches.ContainsKey(address))
        {
            var handle = this.caches[address];
            if (handle.IsDone)
            {
                var result = (AsyncOperationHandle<IList<T>>)this.caches[address].Result;
                List<T> objs = new List<T>();
                for (int i = 0; i < result.Result.Count; i++)
                {
                    var obj = result.Result[i] as T;
                    objs.Add(obj);
                }
                if (onComplete != null)
                {
                    onComplete(objs);
                }
            }
            else
            {
                handle.Completed += (objResult) => {
                    var result = (AsyncOperationHandle<IList<T>>)this.caches[address].Result;
                    if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        List<T> objs = new List<T>();
                        for (int i = 0; i < result.Result.Count; i++)
                        {
                            var obj = result.Result[i] as T;
                            objs.Add(obj);
                        }
                        if (onComplete != null)
                        {
                            onComplete(objs);
                        }
                        if (autoUnload)
                            UnLoadAsset(address);
                    }
                    else
                    {
                        if (onFailed != null)
                        {
                            onFailed();
                        }
                        Helper.LogError("Load " + address + " failed!");
                    }
                };
            }
            return this.caches[address];
        }
        else
        {
            var handle = Addressables.LoadAssetsAsync<T>(new List<object> { address }, null, Addressables.MergeMode.Intersection);
            handle.Completed += (result) => {
                if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    List<T> objs = new List<T>();
                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        var obj = result.Result[i] as T;
                        objs.Add(obj);
                    }
                    if (onComplete != null)
                    {
                        onComplete(objs);
                    }
                    if (autoUnload)
                        UnLoadAsset(address);
                }
                else
                {
                    if (onFailed != null)
                    {
                        onFailed();
                    }
                    Helper.LogError("Load " + address + " failed!");
                }

            };
            addCaches(address, handle);
            return handle;
        }

    }

    /// <param name="address">资源地址</param>
    /// <param name="parent">父节点</param>
    /// <param name="onComplete">完成回调</param>
    public void InstantiateAsset(string address, UnityEngine.Transform parent, System.Action<UnityEngine.Transform> onComplete)
    {
        this.pool.LoadAsset(address, (obj) => {
            obj.gameObject.name = address;
            obj.gameObject.SetActive(false);
            if (parent != null)
            {
                obj.transform.SetParent(parent);
                var prefab = this.pool.GetTemplate(address);
                if (prefab != null)
                {
                    obj.transform.localPosition = prefab.transform.localPosition;
                    obj.transform.localScale = prefab.transform.localScale;
                    obj.transform.localRotation = prefab.transform.localRotation;
                    var rect = obj.GetComponent<RectTransform>();
                    var prefabRect = prefab.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.localScale = prefabRect.localScale;
                        rect.localPosition = prefabRect.localPosition;
                        rect.localRotation = prefabRect.localRotation;
                        rect.anchorMin = prefabRect.anchorMin;
                        rect.anchorMax = prefabRect.anchorMax;
                        rect.offsetMin = prefabRect.offsetMin;
                        rect.offsetMax = prefabRect.offsetMax;
                    }
                }
            }
            if (onComplete != null)
            {
                onComplete(obj);
            }
        });
    }

    public void LoadSprite(string address, System.Action<UnityEngine.Sprite> onComplete, System.Action onFailed)
    {
        LoadAsset<Sprite>(address, onComplete, onFailed, false);
    }

    /// <summary>
    /// 获取下载的资源大小
    /// </summary>
    public void GetDownLoadSize(object key, System.Action<long> onComplete, System.Action onFailed = null)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync(key.ToString());
        sizeHandle.Completed += (result) => {

            if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var totalDownLoadSize = sizeHandle.Result;
                if (onComplete != null)
                {
                    onComplete(totalDownLoadSize);
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }
            }
            Addressables.Release(sizeHandle);
        };
    }
    public void GetDownLoadSize(List<object> keys, System.Action<long> onComplete, System.Action onFailed = null)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync(keys);
        sizeHandle.Completed += (result) => {

            if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var totalDownLoadSize = sizeHandle.Result;
                if (onComplete != null)
                {
                    onComplete(totalDownLoadSize);
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }
            }
            Addressables.Release(sizeHandle);
        };
    }
    /// <remarks>下载指定资源</remarks>
    public AsyncOperationHandle DownLoad(object key, System.Action onComplete, System.Action onFailed = null)
    {
        var downLoadHandle = Addressables.DownloadDependenciesAsync(key.ToString(), true);
        downLoadHandle.Completed += (result) =>
        {
            if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }
            }
        };
        return downLoadHandle;
    }

    public AsyncOperationHandle DownLoad(List<object> keys, System.Action onComplete, System.Action onFailed = null)
    {
        var downLoadHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union, true);
        downLoadHandle.Completed += (result) =>
        {
            if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }
            }
        };
        return downLoadHandle;
    }

    public void UnLoadAsset(string address)
    {
        if (caches.ContainsKey(address))
        {
            Debug.Log("Clear:" + address);
            var handle = caches[address];
            Addressables.Release(handle);
            caches.Remove(address);
        }
    }

    /// <summary>
    /// 释放资源到缓存池
    /// </summary>
    /// <param name="obj"></param>
    public void Free(UnityEngine.Transform obj)
    {
        this.pool.Free(obj);
    }

    /// <summary>
    /// 直接销毁资源
    /// </summary>
    public void Delete(UnityEngine.Transform obj)
    {
        this.pool.Delete(obj);
    }

    private void addCaches(string address, AsyncOperationHandle handle)
    {
        if (!caches.ContainsKey(address))
        {
            caches.Add(address, handle);
        }
    }
}