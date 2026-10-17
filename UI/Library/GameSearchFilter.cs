using System;
using System.Collections.Generic;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.UI.Library
{
    public static class GameSearchFilter
    {
        public static List<GameProfile> FilterProfiles(IEnumerable<GameProfile> allProfiles, string searchQuery, out int totalInstalledCount)
        {
            var profilesList = new List<GameProfile>();
            if (allProfiles != null)
            {
                foreach (var p in allProfiles)
                {
                    if (p.IsInstalled || p.IsCustom || p.IsActive)
                    {
                        profilesList.Add(p);
                    }
                }
            }

            totalInstalledCount = profilesList.Count;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                string query = searchQuery.Trim();
                profilesList = profilesList.FindAll(p =>
                    (p.Name != null && p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.ProcessNames != null && p.ProcessNames.Exists(proc => proc != null && proc.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (p.PrimaryProcessName != null && p.PrimaryProcessName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Category != null && p.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.TargetPacing != null && p.TargetPacing.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.OptimizationBadge != null && p.OptimizationBadge.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            return profilesList;
        }

        public static void SortProfiles(List<GameProfile> profiles, string sortMode, List<string> customOrder)
        {
            if (profiles == null) return;
            string mode = sortMode ?? "Custom";
            var order = customOrder ?? new List<string>();

            if (mode == "Custom" || (mode == "Installed" && order.Count > 0))
            {
                profiles.Sort((a, b) =>
                {
                    int indexA = order.IndexOf(a.Id);
                    int indexB = order.IndexOf(b.Id);
                    if (indexA == -1 && indexB == -1) return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    if (indexA == -1) return 1;
                    if (indexB == -1) return -1;
                    return indexA.CompareTo(indexB);
                });
            }
            else if (mode == "Name")
            {
                profiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (mode == "Favorites")
            {
                profiles.Sort((a, b) =>
                {
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (mode == "Active")
            {
                profiles.Sort((a, b) =>
                {
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else
            {
                profiles.Sort((a, b) =>
                {
                    if (a.IsFavorite != b.IsFavorite) return b.IsFavorite.CompareTo(a.IsFavorite);
                    if (a.IsActive != b.IsActive) return b.IsActive.CompareTo(a.IsActive);
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        public static string FormatCountBadge(int filteredCount, int totalInstalledCount, bool isSearchActive)
        {
            if (isSearchActive)
            {
                return string.Format("{0} of {1} Filtered", filteredCount, totalInstalledCount);
            }
            return string.Format("{0} Installed", filteredCount);
        }
    }
}
