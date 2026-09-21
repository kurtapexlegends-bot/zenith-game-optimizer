using System;
using System.Collections.Generic;

namespace ZenithOptimizer.Models
{
    public class GameProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> ProcessNames { get; set; }
        public string Description { get; set; }
        public string OptimizationBadge { get; set; }
        public string TargetPacing { get; set; }
        public bool IsActive { get; set; }
        public bool EnablePCoreAffinity { get; set; }
        public bool EnableHighPriority { get; set; }
        public bool EnableStandbyPurge { get; set; }
        public bool IsCustom { get; set; }
        public string PrimaryProcessName
        {
            get
            {
                return (ProcessNames != null && ProcessNames.Count > 0) ? ProcessNames[0] : (Id ?? "game");
            }
        }

        public GameProfile()
        {
            ProcessNames = new List<string>();
            EnablePCoreAffinity = false; // Default safe for mobile/hybrid CPUs
            EnableHighPriority = true;
            EnableStandbyPurge = false; // Default safe: do not evict active game RAM
            IsActive = false;
            IsCustom = false;
        }

        public static List<GameProfile> GetDefaultProfiles()
        {
            var list = new List<GameProfile>();

            // 1. Mobile Legends (BlueStacks) - specifically tuned for VM hypervisors
            var bs = new GameProfile();
            bs.Id = "bluestacks";
            bs.Name = "Mobile Legends (BlueStacks)";
            bs.Category = "Android Virtualization / MOBA";
            bs.ProcessNames.Add("HD-Player");
            bs.ProcessNames.Add("BlueStacks");
            bs.ProcessNames.Add("BstkSVC");
            bs.Description = "0.500ms low-latency multimedia timer and balanced AboveNormal priority with PriorityBoost. Never restricts VM core affinity or evicts guest RAM.";
            bs.OptimizationBadge = "0.5ms Timer / VM Safe";
            bs.TargetPacing = "60 / 120 FPS Smooth";
            bs.EnablePCoreAffinity = false; // BlueStacks needs ALL threads for vCPUs
            bs.EnableHighPriority = false;  // AboveNormal prevents starving Windows DWM/audio
            bs.EnableStandbyPurge = false;  // NEVER purge RAM while VM is active
            list.Add(bs);

            // 2. Valorant
            var valo = new GameProfile();
            valo.Id = "valo";
            valo.Name = "Valorant";
            valo.Category = "Competitive Tactical FPS";
            valo.ProcessNames.Add("VALORANT-Win64-Shipping");
            valo.ProcessNames.Add("VALORANT");
            valo.Description = "Riot Vanguard-safe OS scheduling. 0.500ms multimedia timer for microsecond input polling and AboveNormal render thread priority.";
            valo.OptimizationBadge = "Vanguard-Safe / 0.5ms Timer";
            valo.TargetPacing = "Max Refresh (Uncapped)";
            valo.EnablePCoreAffinity = false;
            valo.EnableHighPriority = true;
            valo.EnableStandbyPurge = false;
            list.Add(valo);

            // Assassin's Creed Unity
            var acu = new GameProfile();
            acu.Id = "acu";
            acu.Name = "Assassin's Creed Unity";
            acu.Category = "Action-Adventure / Open World";
            acu.ProcessNames.Add("ACU");
            acu.ProcessNames.Add("acu");
            acu.Description = "AnvilNext engine scheduling and Direct3D 11 draw-call optimization; stabilizes crowd simulation frame pacing in revolutionary Paris.";
            acu.OptimizationBadge = "AnvilNext / 0.5ms Timer";
            acu.TargetPacing = "Stable 60 FPS";
            acu.EnablePCoreAffinity = false;
            acu.EnableHighPriority = true;
            acu.EnableStandbyPurge = false;
            list.Add(acu);

            // 3. League of Legends
            var lol = new GameProfile();
            lol.Id = "lol";
            lol.Name = "League of Legends";
            lol.Category = "Competitive MOBA";
            lol.ProcessNames.Add("League of Legends");
            lol.ProcessNames.Add("LeagueClientUx");
            lol.Description = "Elevates process priority above background services; locks timer to 0.500ms for stable click responsiveness in teamfights.";
            lol.OptimizationBadge = "Priority Boost / 0.5ms Timer";
            lol.TargetPacing = "Stable 144/240+ FPS";
            lol.EnablePCoreAffinity = false;
            lol.EnableHighPriority = true;
            lol.EnableStandbyPurge = false;
            list.Add(lol);

            // 4. Tekken 7
            var tekken = new GameProfile();
            tekken.Id = "tekken7";
            tekken.Name = "Tekken 7";
            tekken.Category = "Competitive Fighting Game";
            tekken.ProcessNames.Add("TekkenGame-Win64-Shipping");
            tekken.ProcessNames.Add("TekkenGame");
            tekken.Description = "Strict 16.66ms frame pacing. Minimizes DPC latency and input queue delay for frame-perfect punishes without dropping frames.";
            tekken.OptimizationBadge = "Frame-Pacing / 0 Drop Lock";
            tekken.TargetPacing = "Locked 60.00 FPS";
            tekken.EnablePCoreAffinity = false;
            tekken.EnableHighPriority = true;
            tekken.EnableStandbyPurge = false;
            list.Add(tekken);

            // 5. Counter-Strike 2
            var cs2 = new GameProfile();
            cs2.Id = "cs2";
            cs2.Name = "Counter-Strike 2";
            cs2.Category = "Esports Tactical Shooter";
            cs2.ProcessNames.Add("cs2");
            cs2.Description = "Source 2 sub-tick packet synchronization optimization; locks multimedia timer to 0.500ms for lowest render dispatch latency.";
            cs2.OptimizationBadge = "Sub-Tick / 0.5ms Sync";
            cs2.TargetPacing = "Max Refresh Rate";
            cs2.EnablePCoreAffinity = false;
            cs2.EnableHighPriority = true;
            cs2.EnableStandbyPurge = false;
            list.Add(cs2);

            // 6. Roblox
            var roblox = new GameProfile();
            roblox.Id = "roblox";
            roblox.Name = "Roblox";
            roblox.Category = "Multiplayer Action / Sandbox";
            roblox.ProcessNames.Add("RobloxPlayerBeta");
            roblox.ProcessNames.Add("RobloxPlayerLauncher");
            roblox.Description = "Gives Roblox priority over background apps and locks input responsiveness to 0.500ms for smooth camera controls and jump timing.";
            roblox.OptimizationBadge = "Smooth Controls / 0.5ms";
            roblox.TargetPacing = "Smooth 60 / Uncapped FPS";
            roblox.EnablePCoreAffinity = false;
            roblox.EnableHighPriority = true;
            roblox.EnableStandbyPurge = false;
            list.Add(roblox);

            // 11. Dota 2
            var dota2 = new GameProfile();
            dota2.Id = "dota2";
            dota2.Name = "Dota 2";
            dota2.Category = "Competitive MOBA";
            dota2.ProcessNames.Add("dota2");
            dota2.Description = "Source 2 rendering priority and low-latency mouse input registration for instant reaction during large teamfights.";
            dota2.OptimizationBadge = "Instant Cast / 0.5ms";
            dota2.TargetPacing = "Smooth 120+ FPS";
            dota2.EnablePCoreAffinity = false;
            dota2.EnableHighPriority = true;
            dota2.EnableStandbyPurge = false;
            list.Add(dota2);

            // 12. Minecraft
            var mc = new GameProfile();
            mc.Id = "minecraft";
            mc.Name = "Minecraft";
            mc.Category = "Sandbox / Survival";
            mc.ProcessNames.Add("javaw");
            mc.ProcessNames.Add("Minecraft.Windows");
            mc.Description = "Reduces chunk loading stutter and GC pause latency by keeping high process priority and full power delivery.";
            mc.OptimizationBadge = "Chunk Smooth / 0.5ms";
            mc.TargetPacing = "Smooth Framerate";
            mc.EnablePCoreAffinity = false;
            mc.EnableHighPriority = true;
            mc.EnableStandbyPurge = false;
            list.Add(mc);

            return list;
        }
    }
}
