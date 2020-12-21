using System.Collections.Generic;
using UnityEngine;
public class AddressablePool<T> where T:Component
{
    /// <summary>
    /// 缓存池
    /// </summary>
    private Dictionary<string, CacheList> pools=new Dictionary<string, CacheList>();
    /// <summary>
    /// 缓存查找表
    /// </summary>
    private Dictionary<T, CacheList> lookup=new Dictionary<T, CacheList>();
    /// <summary>
    /// 缓存对象根节点
    /// </summary>
    private UnityEngine.Transform root;
    /// <summary>
    /// 标记是否正在加载资源
    /// </summary>
    private bool isLoad;

    private Dictionary<string, List<System.Action<T>>> loadCallBack=new Dictionary<string, List<System.Action<T>>>();

    public AddressablePool(UnityEngine.Transform root, bool addClear=true)
    {
        this.root = root;
        loadCallBack.Clear();
        if (addClear)
        {
            //如果需要自己定时清理，添加清理事件
            AddressableManager.Instance.ClearCaches += clearCache;
        }

    }

    public void clearCache()
    {
        if (isLoad)
            return;//如果正在进行加载的操作则不清理，直接返回
        InitInformation();
        foreach (var info in this.pools)
        {
            List<T> temps = info.Value.GetClearCaches();
            if (temps != null)
            {
                foreach (var obj in temps)
                {
                    this.lookup.Remove(obj);
                    GameObject.Destroy(obj.gameObject);
                }
            }
        }
        List<string> deleteList = new List<string>();
        foreach (var info in this.pools)
        {
            if (info.Value.Count == 0)
            {
                info.Value.ClearCaches(this.lookup);
                deleteList.Add(info.Key);
            }
        }
        foreach (var info in deleteList)
        {
            this.pools.Remove(info);
        }
    }

    public void LoadAsset(string address, System.Action<T> onComplete, float Exceed=AddressableManager.Exceed)
    {
        isLoad = true;
        CacheList cache;
        if (this.pools.TryGetValue(address, out cache))
        {
            isLoad = false;
            var obj = LoadAssetByTemplate(address);
            if (obj != null)
            {
                onComplete(obj);
            }
            else
            {
                Helper.LogError("加载资源失败，请速查：" + address);
            }
        }
        else
        {
            List<System.Action<T>> callBackList;
            if (loadCallBack.TryGetValue(address, out callBackList))
            {
                ///改地址资源已经在加载中，为避免二次加载
                ///直接缓存加载完成回调
                callBackList.Add(onComplete);
            }
            else
            {
                callBackList = new List<System.Action<T>>();
                callBackList.Add(onComplete);
                loadCallBack.Add(address, callBackList);
                AddressableManager.Instance.LoadAsset<GameObject>(address, (objAsset) =>
                {
                    AddTemplate(address, objAsset, true, Exceed);
                    var list = loadCallBack[address];
                    if (objAsset != null)
                    {
                        if (list != null && list.Count > 0)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                var obj = LoadAssetByTemplate(address);
                                list[i](obj);
                            }
                        }
                    }
                    else
                    {
                        Helper.LogError("加载资源失败，请复查：" + address);
                    }

                }, () => {
                    Helper.LogError("加载资源失败，请检查：" + address);
                });
                
            }
        }

    }

    public T LoadAssetByTemplate(string address)
    {
        CacheList cache;
        if (this.pools.TryGetValue(address, out cache))
        {
            if (cache.CachesCount > 0)
            {
                T instance = cache.Pop() as T;
                if (instance != null)
                {
                    isLoad = false;
                    instance.gameObject.SetActive(false);
                    return instance;
                }
                else
                {
                    Helper.LogError(GetType() + "/Load Asset " + address + " is Failed!");
                }
            }
            else
            {
                var _obj = this.Instantiate(cache);
                isLoad = false;
                return _obj;
            }
        }
        else
        {
            Helper.LogError("请先通过AddTemplate方法设置模板信息");
        }
        return null;
    }

    /// <summary>
    /// 添加模板
    /// 默认走资源加载流程
    /// 模板若来源于已经存在的对象则isAddressabel为false，主要用于在具体的界面时使用缓存池
    /// </summary>
    public void AddTemplate(string address, UnityEngine.Object obj, bool isAddressabel=true, float Exceed = AddressableManager.Exceed)
    {
        if (!this.pools.ContainsKey(address))
        {
            CacheList cache = new CacheList();
            cache.Address = address;
            cache.Prefab = obj;
            cache.Exceed = Exceed;
            cache.isAddressableAssets = isAddressabel;
            isLoad = false;
            this.pools.Add(address, cache);
        }
        else
        {
            Helper.LogError("速查，出现同名资源：" + address);
        }
    }

    public UnityEngine.GameObject GetTemplate(string address)
    {
        CacheList cache;
        if (this.pools.TryGetValue(address, out cache))
        {
            if (cache.Prefab != null)
            {
                return cache.Prefab as GameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// 清理缓存池
    /// </summary>
    public void Clear()
    {
        foreach (var info in this.pools)
        {
            var cache = info.Value;
            cache.ClearCaches(this.lookup);
        }
        this.pools.Clear();
        this.lookup.Clear();
    }

    /// <summary>
    /// 释放对象到缓存池
    /// </summary>
    public void Free(T instance)
    {
        CacheList cache;
        if (!this.lookup.TryGetValue(instance, out cache))
        {
            Helper.LogWarning("Try to free an instance not allocated by this pool");
            GameObject.Destroy(instance.gameObject);
            return;
        }
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(this.root);
        cache.Push(instance);
    }

    /// <param name="instance">直接销毁对象</param>
    public void Delete(T instance)
    {
        CacheList cache;
        if (this.lookup.TryGetValue(instance, out cache))
        {
            cache.Delete(instance);
            this.lookup.Remove(instance);
        }
        if (cache.Count == 0)
        {
            cache.ClearCaches(this.lookup);
            this.pools.Remove(cache.Address);
        }
    }

    private T Instantiate(CacheList cache)
    {
        UnityEngine.Object obj = GameObject.Instantiate(cache.Prefab);
        GameObject gameObject = obj as GameObject;
        if(gameObject==null)
        {
            Helper.LogError(cache.Address + " is not a GameObject Asset");
        }
        gameObject.SetActive(false);
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            Helper.LogError(GetType() + string.Format("/The object {1} has no {0} component.", typeof(T).Name, obj.name));
            return null;
        }
        this.lookup.Add(component, cache);
        cache.AddReference(component);
        component.gameObject.name = cache.Address;
        component.gameObject.SetActive(false);
        component.transform.SetParent(this.root);
        component.transform.localScale = Vector3.one;
        return component;
    }

    /// <summary>
    /// 重置使用计数
    /// </summary>
    internal void InitInformation()
    {
        foreach (var info in pools)
        {
            info.Value.InitInformation();
        }
    }

    private class CacheList
    {
        /// <summary>
        /// 资源地址
        /// </summary>
        public string Address;
        /// <summary>
        /// 资源缓存列表
        /// </summary>
        private Stack<T> caches;
        /// <summary>
        /// 引用列表
        /// </summary>
        private HashSet<T> references;
        /// <summary>
        /// 自动清理参数
        /// </summary>
        public float Exceed;
        /// <summary>
        /// 资源模板
        /// </summary>
        public UnityEngine.Object Prefab;
        private int maxReferenceNumber;
        /// <summary>
        /// 确定是预制还是Unity资源
        /// </summary>
        public bool isAddressableAssets;
        private Dictionary<string, List<System.Action<T>>> loadCallBack;

        /// <summary>
        /// 缓存计数总和
        /// </summary>
        public int Count
        {
           get
            {
                return this.references.Count + this.caches.Count;
            }
        }

        /// <summary>
        /// 可以使用的缓存对象的数量
        /// </summary>
        public int CachesCount
        {
            get
            {
                return this.caches.Count;
            }
        }

        /// <summary>
        /// 添加使用项
        /// </summary>
        public void AddReference(T t)
        {
            if (this.references.Add(t))
            {
                this.maxReferenceNumber = this.references.Count > this.maxReferenceNumber ? this.references.Count : this.maxReferenceNumber;
            }
        }

        /// <summary>
        /// 从缓存池中取出一个缓存对象使用
        /// </summary>
        public T Pop()
        {
            if (this.caches.Count > 0)
            {
                T _reference = this.caches.Pop();
                this.AddReference(_reference);
                return _reference;
            }
            else
            {
                throw new System.Exception(string.Format("{0} CacheList's caches is empty but use pop", Prefab.name));
            }
        }

        /// <summary>
        /// 释放时将对象放回缓存池，以便复用
        /// </summary>
        public void Push(T t)
        {
            this.caches.Push(t);
            this.references.Remove(t);
            InitInformation();
        }

        /// <summary>
        /// 直接删除使用的对象，而不放回缓存池
        /// </summary>
        public void Delete(T t)
        {
            this.references.Remove(t);
            GameObject.Destroy(t.gameObject);
        }

        /// <summary>
        /// 重置使用计数
        /// </summary>
        internal void InitInformation()
        {
            this.maxReferenceNumber = this.references.Count;
        }

        /// <summary>
        /// 缓存清理
        /// </summary>
        public void ClearCaches(Dictionary<T, CacheList> lookup)
        {
            foreach (var obj in this.caches)
            {
                lookup.Remove(obj);
                GameObject.Destroy(obj.gameObject);
            }
            foreach (var obj in this.references)
            {
                lookup.Remove(obj);
                GameObject.Destroy(obj.gameObject);
            }
            this.caches.Clear();
            this.references.Clear();
            Prefab = null;
            if (isAddressableAssets)
                AddressableManager.Instance.UnLoadAsset(Address);
        }

        /// <summary>
        /// 计算需要清理的缓存数量
        /// </summary>
        public List<T> GetClearCaches()
        {
            int clearCount = this.Count - ((int)(this.maxReferenceNumber * Exceed));
            if (clearCount > 0)
            {
                List<T> list = new List<T>();
                for (int i = 0; i < clearCount; i++)
                {
                    list.Add(this.caches.Pop());
                }
                return list;
            }
            return null;
        }
    }
}