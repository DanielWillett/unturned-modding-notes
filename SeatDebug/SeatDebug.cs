/*
 * Author: Daniel Willett (https://github.com/DanielWillett)
 * Date: 2026/05/13
 * Source: https://github.com/DanielWillett/unturned-modding-notes/blob/main/SeatDebug/SeatDebug.cs
 * Part of this package: https://github.com/DanielWillett/unturned-modding-notes/blob/main/SeatDebug.unitypackage
 * Version: 1.0
 *
 * Visualizes player locations based on their seats.
 *
 */

using System.Globalization;
using UnityEngine;

#pragma warning disable IDE0130
namespace DanielWillett
#pragma warning restore IDE0130
{
    /// <summary>
    /// Renders the player on a seat.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class SeatDebug : MonoBehaviour
    {
#if UNITY_EDITOR

        private bool _meshIsReclined;

        [Tooltip("Mesh rendered for sitting players.")]
        public Mesh sittingMesh;

        [Tooltip("Mesh rendered for reclined players.")]
        public Mesh reclinedMesh;

        [Tooltip("Whether or not this seat is reclined. Only has an effect on the driver seat.")]
        public bool reclined;

        private static readonly Color RenderColor               = new Color32(127, 102, 076, 64);
        private static readonly Color DriverRenderColor         = new Color32(210, 180, 180, 64);
        private static readonly Color TurretRenderColor         = new Color32(255, 204, 120, 64);
        private static readonly Color RemoteTurretRenderColor   = new Color32(230, 179, 255, 64);

        private Mesh GetMesh(bool reclined)
        {
            Mesh mesh = reclined ? reclinedMesh : sittingMesh;
            if (mesh == null)
            {
                Debug.LogError($"{(reclined ? "Reclined" : "Sitting")} mesh not found.");
            }

            return mesh;
        }

        private void OnDrawGizmos()
        {
            Transform parent = transform.parent;
            string name = parent != null ? parent.gameObject.name : null;
            bool isDriver = parent != null && name == "Seat_0";

            Mesh mesh = GetMesh(isDriver && reclined);
            if (mesh == null)
            {
                return;
            }

            Color color = isDriver ? DriverRenderColor : RenderColor;

            Transform turretSeat = null;

            Transform aim = null;
            bool isTurret = false;
            Transform turretList = parent != null ? parent.root.Find("Turrets") : null;
            if (name != null
                && name.StartsWith("Seat_")
                && int.TryParse(name.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out int id)
                && turretList != null)
            {
                Transform correspondingTurret = turretList.Find("Turret_" + id.ToString(CultureInfo.InvariantCulture));
                if (correspondingTurret != null)
                {
                    isTurret = true;
                    if (!isDriver)
                        color = TurretRenderColor;
                    aim = FindChildRecursive(correspondingTurret, "Aim");

                    Transform yaw = correspondingTurret.Find("Yaw");
                    if (yaw != null)
                    {
                        Transform yawSeats = yaw.Find("Seats");
                        if (yawSeats != null)
                        {
                            turretSeat = yawSeats.Find(name);
                        }
                        else
                        {
                            Transform pitch = yaw.Find("Pitch");
                            if (pitch != null)
                            {
                                Transform pitchSeats = pitch.Find("Seats");
                                if (pitchSeats != null)
                                {
                                    turretSeat = pitchSeats.Find(name);
                                }
                            }
                        }

                        if (turretSeat != null)
                        {
                            color = RemoteTurretRenderColor;
                        }
                    }
                }
            }

            Matrix4x4 old = Gizmos.matrix;
            Color oldColor = Gizmos.color;

            Gizmos.color = color;

            Matrix4x4 matrix = (turretSeat != null ? turretSeat : transform).localToWorldMatrix;
            Gizmos.matrix = matrix;

            Gizmos.DrawMesh(mesh);
            Gizmos.DrawRay(new Vector3(0f, 1.6f, 0.215f), Vector3.forward * (isDriver ? 1f : 0.35f));

            Gizmos.matrix = old;

            if (isTurret && aim != null)
            {
                Gizmos.DrawRay(aim.position, aim.forward);
            }

            Gizmos.color = oldColor;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            // copied from Unturned
            int childCount = parent.childCount;
            for (int index = 0; index < childCount; ++index)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name)
                    return child;
                if (child.childCount != 0)
                {
                    Transform childRecursive = FindChildRecursive(child, name);
                    if (childRecursive != null)
                        return childRecursive;
                }
            }
            return null;
        }
#endif
    }
}