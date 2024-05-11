using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace noya
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class PlanetaryShield : MySessionComponentBase
    {
        bool initialized = false;
        bool checkGravity = false;
        List<BoundingSphere> spheres = new List<BoundingSphere>();
        public override void UpdateBeforeSimulation()
        {

            if (!initialized)
            {
                init();
            }
        }

        void init()
        {
            try
            {
                spheres.Clear();
                // spheres.Add(new BoundingSphere(
                //     new Vector3(0, 0, 0), //星球中心
                //     120000f//区域半径
                //     ));
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, PlanetaryDamageHandler);
                initialized = true;
            }
            catch (Exception) { }

        }

        void PlanetaryDamageHandler(object target, ref MyDamageInformation info)
        {
            try
            {
                IMySlimBlock block = target as IMySlimBlock;
                long id = 0L;

                if (block != null)
                {
                    id = block.CubeGrid.BigOwners.Count() > 0 ? block.CubeGrid.BigOwners[0] : 0L;
                }
                else return;

                long attackerId = info.AttackerId;
                IMyEntity attacker;
                MyAPIGateway.Entities.TryGetEntityById(attackerId, out attacker);

                if (attacker is IMyHandheldGunObject<MyGunBase>)
                {
                    attackerId = (attacker as IMyHandheldGunObject<MyGunBase>).OwnerIdentityId;
                } else if (attacker is IMyHandheldGunObject<MyToolBase>)
                {
                    attackerId = (attacker as IMyHandheldGunObject<MyToolBase>).OwnerIdentityId;
                }

                // MyAPIGateway.Utilities.ShowNotification("Detected a player being attacked with Steam ID: " + MyAPIGateway.Multiplayer.Players.TryGetSteamId(id), 2000, MyFontEnum.Green);

                if (id == 0L || MyAPIGateway.Multiplayer.Players.TryGetSteamId(id) == 0UL || id == attackerId)
                {
                    return;
                }
                else if (attacker is IMyCubeBlock)
                { // when attack source is a block
                    var attackerWeaponBlock = attacker as IMyCubeBlock;
                    var attackerGridOwners = attackerWeaponBlock.CubeGrid.BigOwners;
                    if (attackerGridOwners.Count() > 0)
                    { // when the grid has a owner
                        var mainOwner = attackerGridOwners[0];
                        var mainOwnerSteamID = MyAPIGateway.Multiplayer.Players.TryGetSteamId(mainOwner);
                        if (mainOwnerSteamID == 0UL)
                        { // attack owner is NPC
                            List<IMyPlayer> onlinePlayers = new List<IMyPlayer>();
                            MyAPIGateway.Multiplayer.Players.GetPlayers(onlinePlayers);
                            if (!onlinePlayers.Exists(p => p.IdentityId == id && !p.IsBot)) info.Amount = info.Amount / 10;
                            return;
                        }
                    }
                }

                bool inSphere = (spheres.Count == 0);
                for (int i = 0; i < spheres.Count; i++)
                {
                    if (Vector3D.Distance(block.CubeGrid.PositionComp.GetPosition(), spheres[i].Center) < spheres[i].Radius)
                        inSphere = true;
                }

                if ((!checkGravity || block.CubeGrid.Physics.Gravity != new Vector3D()) && inSphere)
                {
                    if (attacker is IMyVoxelBase)
                    {
                        info.Amount = info.Amount / 5;
                    }
                    else
                    {
                        info.Amount = 0f;
                        info.IsDeformation = false;
                        MyAPIGateway.Utilities.ShowNotification("PVE区不能互相伤害", 2000, MyFontEnum.Green);
                    }
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Error: " + e);
            }

        }
    }
}
