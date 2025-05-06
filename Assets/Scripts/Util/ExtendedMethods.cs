using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ExtendedMethods
{
    /// <summary>
    /// stringのListの値を全てLogする
    /// </summary>
    public static void Print<T>(this List<T> list)
    {
        foreach (var s in list)
        {
            Debug.Log(s);
        }
    }
    
    /// <summary>
    /// enumを順番に切り替えた結果を返す 
    /// </summary>
    public static T Toggle<T>(this T current) where T : System.Enum
    {
        var values = (T[])System.Enum.GetValues(current.GetType());
        var index = System.Array.IndexOf(values, current);
        var nextIndex = (index + 1) % values.Length;
        return values[nextIndex];
    }

    /// <summary>
    /// Selectableのリストに対してナビゲーションを設定する
    /// </summary>
    public static void SetNavigation(this List<Selectable> selectables, bool isHorizontal = true)
    {
        for (var i = 0; i < selectables.Count; i++)
        {
            var selectable = selectables[i];
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;

            if (isHorizontal)
            {
                navigation.selectOnLeft = i == 0 ? null : selectables[i - 1];
                navigation.selectOnRight = i == selectables.Count - 1 ? null : selectables[i + 1];
            }
            else
            {
                navigation.selectOnUp = i == 0 ? null : selectables[i - 1];
                navigation.selectOnDown = i == selectables.Count - 1 ? null : selectables[i + 1];
            }

            selectable.navigation = navigation;
        }
    }
    
    /// <summary>
    /// 全てのEventTriggerを削除する
    /// </summary>
    public static void RemoveAllEventTrigger(this GameObject g)
    {
        if (!g) return;
        var eventTrigger = g.GetComponent<EventTrigger>();
        if (!eventTrigger) return;
        
        foreach (var entry in eventTrigger.triggers)
        {
            eventTrigger.triggers.Remove(entry);
        }
    }
}