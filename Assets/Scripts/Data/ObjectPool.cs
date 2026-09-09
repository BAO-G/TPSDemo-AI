using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池（静态类）：键 = 预制体，栈式回收 + 可选预热
/// 供特效/弹痕/曳光/手雷等高频表现层对象复用
/// 约束：池化对象脚本不得在 OnDisable 中破坏状态；Release 后不得再引用
/// </summary>
public static class ObjectPool
{
    /// <summary>记录实例来源预制体的票据组件（Release 时按此回池）</summary>
    public class PoolTicket : MonoBehaviour
    {
        [HideInInspector] public GameObject sourcePrefab;
    }

    private static readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private static Transform _root;

    /// <summary>池根节点；场景切换被销毁时清空全部池（引用已失效）</summary>
    private static Transform Root
    {
        get
        {
            if (_root == null)
            {
                _pools.Clear();
                var go = new GameObject("[ObjectPool]");
                _root = go.transform;
                Object.DontDestroyOnLoad(go);
            }
            return _root;
        }
    }

    /// <summary>预热：预先实例化 count 个 inactive 实例入池</summary>
    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        var queue = GetQueue(prefab);
        for (int i = 0; i < count; i++)
        {
            var inst = CreateInstance(prefab);
            inst.SetActive(false);
            queue.Enqueue(inst);
        }
    }

    /// <summary>取实例：池空则新建；调用方负责用完后 Release</summary>
    public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        var queue = GetQueue(prefab);

        GameObject inst;
        do
        {
            inst = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);
            // 队列中可能残留已销毁对象（场景切换边界），跳过
        } while (inst == null && queue.Count > 0);

        if (inst == null) inst = CreateInstance(prefab);

        inst.transform.SetParent(null, false);
        inst.transform.SetPositionAndRotation(pos, rot);
        inst.SetActive(true);
        return inst;
    }

    /// <summary>还实例入池（挂 PoolTicket 记录来源预制体）；非池化对象直接销毁</summary>
    public static void Release(GameObject instance)
    {
        if (instance == null) return;
        var ticket = instance.GetComponent<PoolTicket>();
        if (ticket == null || ticket.sourcePrefab == null)
        {
            Object.Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(Root, false);
        GetQueue(ticket.sourcePrefab).Enqueue(instance);
    }

    private static Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }
        return queue;
    }

    private static GameObject CreateInstance(GameObject prefab)
    {
        var inst = Object.Instantiate(prefab, Root);
        if (inst.GetComponent<PoolTicket>() == null)
            inst.AddComponent<PoolTicket>().sourcePrefab = prefab;
        return inst;
    }
}
