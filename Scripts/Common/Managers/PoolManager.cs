using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public enum SceneType
    {
        Once, DontDestroy
    }

    public static Dictionary<string, Pool<GameObject>> AllPools = new Dictionary<string, Pool<GameObject>>();
    public SceneType TypeLoad;
    public GameObject pools;

    #region Singleton
    private static PoolManager instance = null;
    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PoolManager();
            }

            return instance;
        }
    }    
    #endregion

    private void Awake()
    {
        InitPools();
        SetTypeManager();
    }

    void SetTypeManager()
    {
        switch (TypeLoad)
        {
            case SceneType.Once:
                {
                    break; 
                }
            case SceneType.DontDestroy:
                {
                    if (!instance)
                        DontDestroyOnLoad(this);
                    else Destroy(this);
                    break;
                }
        }
    }

    void InitPools()
    {
        if (pools == null)
        {
            pools = new GameObject();
            pools.name = "@Pools";
        }
    }

    public void PoolInstaller(GameObject prefab, int size, string groupName)
    {
        var rootTransform = new GameObject();
        rootTransform.transform.SetParent(pools.transform, false); //
        rootTransform.name = string.Format("@{0} Pool", prefab.name);
        var pool = new Pool<GameObject>(() => { return InstantiateObject(prefab, rootTransform); }, size);
        AllPools.Add(groupName, pool);
    }

    public void SpawnObject(GameObject prefab, string groupName)
    {
        if (AllPools.ContainsKey(groupName))
        {
            var objectFromPool = AllPools[groupName].GetFromPool();
            objectFromPool.SetActive(true);
        }
        else
        {
            PoolInstaller(prefab, 1, groupName);
        }
    }

    public void BackToPool(GameObject item, string groupName)
    {
        if (AllPools.ContainsKey(groupName))
        {
            item.SetActive(false);
            AllPools[groupName].BackToPool(item);
        }
    }

    private GameObject InstantiateObject(GameObject prefab, GameObject rootTransform)
    {
        var newObject = Instantiate(prefab) as GameObject;
        newObject.transform.SetParent(rootTransform.transform);
        newObject.SetActive(false);
        return newObject;
    }
}