using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZenithOptimizer.Models;

namespace ZenithOptimizer.UI.Library
{
    public class GameDragDropManager
    {
        private Point _dragStartPos;
        private GameProfile _draggedProfile;
        private readonly Action<string, string> _onReorder;

        public GameDragDropManager(Action<string, string> onReorder)
        {
            _onReorder = onReorder;
        }

        public void Attach(Border itemElement, GameProfile profile)
        {
            if (itemElement == null || profile == null) return;

            itemElement.AllowDrop = true;
            Brush defaultBorderBrush = itemElement.BorderBrush;
            Thickness defaultBorderThickness = itemElement.BorderThickness;

            itemElement.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (IsInteractiveOrToolsElement((DependencyObject)e.OriginalSource)) return;
                _dragStartPos = e.GetPosition(null);
                _draggedProfile = profile;
            };

            itemElement.PreviewMouseLeftButtonUp += (s, e) =>
            {
                _draggedProfile = null;
            };

            itemElement.PreviewMouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedProfile == profile)
                {
                    Point curPos = e.GetPosition(null);
                    if (Math.Abs(curPos.X - _dragStartPos.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(curPos.Y - _dragStartPos.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        _draggedProfile = null;
                        DataObject dragData = new DataObject("ZenithGameDragId", profile.Id);
                        DragDrop.DoDragDrop(itemElement, dragData, DragDropEffects.Move);
                    }
                }
            };

            itemElement.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    string sourceId = (string)e.Data.GetData("ZenithGameDragId");
                    if (!string.IsNullOrEmpty(sourceId) && sourceId != profile.Id)
                    {
                        itemElement.BorderBrush = (SolidColorBrush)Application.Current.FindResource("AccentAction");
                        itemElement.BorderThickness = new Thickness(1.5);
                    }
                }
            };

            itemElement.DragLeave += (s, e) =>
            {
                itemElement.BorderBrush = defaultBorderBrush;
                itemElement.BorderThickness = defaultBorderThickness;
            };

            itemElement.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    e.Effects = DragDropEffects.Move;
                    e.Handled = true;
                }
            };

            itemElement.Drop += (s, e) =>
            {
                itemElement.BorderBrush = defaultBorderBrush;
                itemElement.BorderThickness = defaultBorderThickness;
                if (e.Data.GetDataPresent("ZenithGameDragId"))
                {
                    string sourceId = (string)e.Data.GetData("ZenithGameDragId");
                    if (!string.IsNullOrEmpty(sourceId) && sourceId != profile.Id && _onReorder != null)
                    {
                        _onReorder(sourceId, profile.Id);
                    }
                }
            };
        }

        public static bool Reorder(OptimizationSettings settings, List<GameProfile> profiles, string sourceId, string targetId, out string movedGameName)
        {
            movedGameName = sourceId;
            if (settings == null || profiles == null) return false;

            var order = settings.CustomGameOrder ?? new List<string>();

            foreach (var p in profiles)
            {
                if ((p.IsInstalled || p.IsCustom || p.IsActive) && !order.Contains(p.Id))
                {
                    order.Add(p.Id);
                }
            }

            if (!string.IsNullOrEmpty(sourceId) && !order.Contains(sourceId)) order.Add(sourceId);
            if (!string.IsNullOrEmpty(targetId) && !order.Contains(targetId)) order.Add(targetId);

            int srcIdx = order.IndexOf(sourceId);
            int tgtIdx = order.IndexOf(targetId);
            if (srcIdx >= 0 && tgtIdx >= 0 && srcIdx != tgtIdx)
            {
                order.RemoveAt(srcIdx);
                order.Insert(tgtIdx, sourceId);
                settings.CustomGameOrder = order;
                settings.GameSortMode = "Custom";
                settings.Save();

                var moved = profiles.Find(p => p.Id == sourceId);
                if (moved != null) movedGameName = moved.Name;
                return true;
            }
            return false;
        }

        public static bool IsInteractiveOrToolsElement(DependencyObject element)
        {
            while (element != null)
            {
                if (element is System.Windows.Controls.Primitives.ButtonBase ||
                    element is TextBox ||
                    element is ComboBox ||
                    element is ComboBoxItem ||
                    element is Slider ||
                    element is System.Windows.Controls.Primitives.ScrollBar ||
                    element is CheckBox ||
                    element is RadioButton)
                {
                    return true;
                }

                FrameworkElement fe = element as FrameworkElement;
                if (fe != null && (fe.Tag as string == "ToolsPanel" || fe.Name == "ToolsPanel"))
                {
                    return true;
                }

                if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                {
                    element = VisualTreeHelper.GetParent(element);
                }
                else
                {
                    element = LogicalTreeHelper.GetParent(element);
                }
            }
            return false;
        }
    }
}
