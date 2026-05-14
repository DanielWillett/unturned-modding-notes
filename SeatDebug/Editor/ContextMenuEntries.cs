/*
 * Author: Daniel Willett (https://github.com/DanielWillett)
 * Date: 2026/05/13
 * Source: https://github.com/DanielWillett/unturned-modding-notes/blob/main/SeatDebug/Editor/ContextMenuEntries.cs
 * Part of this package: https://github.com/DanielWillett/unturned-modding-notes/blob/main/SeatDebug.unitypackage
 * Version: 1.0
 *
 * Visualizes player locations based on their seats.
 *
 */

using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable IDE0130
namespace DanielWillett
#pragma warning restore IDE0130
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class ContextMenuEx : MonoBehaviour
    {
        private const string Home = "GameObject/Unturned/";
        private const string PrefabPath = "Assets/BlazingFlame/SeatDebug/Seat_Slot.prefab";
        private const int Priority = -100;

        private const int LayerVehicle = 26;


        [MenuItem(Home + "Add Seat", false, Priority)]
        public static void CreateSittingSeat() => CreateNewSeat(reclined: false);

        [MenuItem(Home + "Add Reclined Seat", false, Priority)]
        public static void CreateReclinedSeat() => CreateNewSeat(reclined: true);

        private static void CreateNewSeat(bool reclined)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (obj is not GameObject prefab)
            {
                Debug.LogError($"Prefab not found at \"{PrefabPath}\". If you moved it, update the 'PrefabPath' constant in this file.");
                return;
            }

            Transform selectedTransform = Selection.activeTransform;
            if (selectedTransform == null)
            {
                Debug.LogError("Nothing is selected.");
                return;
            }

            Transform rootVehicle = selectedTransform.root;

            //Undo.RecordObject(rootVehicle.gameObject, "Add seat");

            Transform rootSeats = rootVehicle.Find("Seats");

            int seatIndex = 0;

            if (TryGetPrefixedIndex(selectedTransform.gameObject.name, "Seat_", out int existingIndex))
            {
                seatIndex = existingIndex;
            }
            else if (rootSeats != null)
            {
                seatIndex = FindNextSeat(rootSeats);
            }

            if (selectedTransform != rootVehicle && selectedTransform != rootSeats)
            {
                for (Transform parent = selectedTransform; parent != null; parent = parent.parent)
                {
                    string name = parent.gameObject.name;
                    if (!name.Equals("Yaw", StringComparison.Ordinal) && !name.Equals("Pitch", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int turretIndex = -1;
                    for (Transform parent2 = selectedTransform; parent2 != null; parent2 = parent2.parent)
                    {
                        if (!TryGetPrefixedIndex(parent2.gameObject.name, "Turret_", out int i))
                            continue;

                        turretIndex = i;
                        break;
                    }

                    if (turretIndex == -1)
                    {
                        break;
                    }

                    seatIndex = turretIndex;

                    Transform turretSeats = parent.Find("Seats");
                    if (turretSeats == null)
                    {
                        turretSeats = new GameObject("Seats").transform;
                        turretSeats.SetParent(parent, false);
                        turretSeats.gameObject.layer = LayerVehicle;
                        turretSeats.gameObject.tag = "Vehicle";
                        Undo.RegisterCreatedObjectUndo(turretSeats.gameObject, "Create missing turret root seats object.");
                    }

                    string turretSeatName = "Seat_" + seatIndex.ToString(CultureInfo.InvariantCulture);
                    Transform turretSeat = turretSeats.Find(turretSeatName);
                    if (turretSeat == null)
                    {
                        turretSeat = new GameObject(turretSeatName).transform;
                        turretSeat.SetParent(turretSeats, false);
                        turretSeat.gameObject.layer = LayerVehicle;
                        turretSeat.gameObject.tag = "Vehicle";
                        Undo.RegisterCreatedObjectUndo(turretSeat.gameObject, $"Create Turret Seat #{seatIndex}.");
                    }

                    break;
                }
            }

            string seatName = "Seat_" + seatIndex.ToString(CultureInfo.InvariantCulture);

            if (rootSeats == null)
            {
                rootSeats = new GameObject("Seats").transform;
                rootSeats.SetParent(rootVehicle, false);
                rootSeats.gameObject.layer = LayerVehicle;
                rootSeats.gameObject.tag = "Vehicle";

                Transform objs = rootVehicle.Find("Objects");
                if (objs != null)
                {
                    rootSeats.SetSiblingIndex(objs.GetSiblingIndex());
                }

                Undo.RegisterCreatedObjectUndo(rootSeats.gameObject, "Create missing root seats object.");
            }

            Transform existingSeat = rootSeats.Find(seatName);

            if (existingSeat != null)
            {
                if (reclined && seatIndex != 0)
                {
                    Debug.LogWarning("Can not make a non-driver seat reclined.");
                    reclined = false;
                }

                selectedTransform.GetLocalPositionAndRotation(out Vector3 oldPos, out Quaternion oldRot);
                Vector3 oldScl = selectedTransform.localScale;

                GameObject replacementSeat = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                replacementSeat.name = seatName;
                replacementSeat.gameObject.layer = LayerVehicle;
                replacementSeat.gameObject.tag = "Vehicle";
                replacementSeat.transform.parent = rootSeats;
                replacementSeat.transform.SetLocalPositionAndRotation(oldPos, oldRot);
                if (oldScl != Vector3.one)
                {
                    replacementSeat.transform.localScale = oldScl;
                }

                if (reclined)
                {
                    replacementSeat.GetComponentInChildren<SeatDebug>().reclined = true;
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(replacementSeat);

                int siblingIndex = selectedTransform.GetSiblingIndex();

                Undo.DestroyObjectImmediate(selectedTransform.gameObject);

                replacementSeat.transform.SetSiblingIndex(siblingIndex);

                Undo.RegisterCreatedObjectUndo(replacementSeat, $"Replace Seat #{existingIndex} with prefab.");

                Undo.IncrementCurrentGroup();
                Selection.activeTransform = replacementSeat.transform;
                return;
            }

            GameObject newSeat = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rootSeats);
            newSeat.name = seatName;
            newSeat.gameObject.layer = LayerVehicle;
            newSeat.gameObject.tag = "Vehicle";

            if (reclined)
            {
                if (seatIndex != 0)
                {
                    Debug.LogWarning("Can not make a non-driver seat reclined.");
                }
                else
                {
                    newSeat.GetComponentInChildren<SeatDebug>().reclined = true;
                }
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(newSeat);

            Undo.RegisterCreatedObjectUndo(newSeat, $"Create Seat #{seatIndex}.");

            if (PrefabUtility.IsAnyPrefabInstanceRoot(rootVehicle.gameObject) || PrefabUtility.IsPartOfPrefabAsset(rootVehicle.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(rootVehicle.gameObject);
            }

            Undo.IncrementCurrentGroup();
        }

        private static bool TryGetPrefixedIndex(string name, string prefix, out int index)
        {
            index = 0;
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
                return false;

            return int.TryParse(name.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        private static int FindNextSeat(Transform rootSeats)
        {
            for (int i = 0; ; ++i)
            {
                Transform seat = rootSeats.Find("Seat_" + i.ToString(CultureInfo.InvariantCulture));
                if (seat == null)
                    return i;
            }
        }
    }
}