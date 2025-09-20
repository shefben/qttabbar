using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace QTTabBarLib
{
    public static class ShellColors
    {
        public static Color LightModeColor = Color.White;

        public static Color ControlMainColor = Color.FromArgb(41, 128, 204);

        public static Color NightModeColor = Color.Black;

        public static Color NightModeTreeViewBackColor = Color.FromArgb(25, 25, 25);

        public static Color NightModeLightColor = Color.FromArgb(43, 43, 43);

        public static Color NightModeTextColor = Color.White;

        public static Color NightModeBorderColor = Color.FromArgb(83, 83, 83);

        public static Color NightModeDisabledColor = Color.FromArgb(140, 140, 140);

        public static Color NightModeTabColor = Color.FromArgb(217, 217, 217);

        public static Color NightModeTextShadow = Color.Gray;

        public static Color NightModeViewBackColor = Color.FromArgb(32, 32, 32);

        public static Color NightModeViewSelectionColor = Color.FromArgb(98, 98, 98);

        public static Color NightModeViewSelectionColorInactive = Color.FromArgb(51, 51, 51);

        public static Color NightModeViewSelectedAndFocusedColor = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewSelectedAndHiliteColor = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewSelectedAndHiliteColorInactive = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewHiliteColor = Color.FromArgb(77, 77, 77);

        public static Color NightModeViewHeaderHiliteColor = Color.FromArgb(67, 67, 67);

        public static Color FaceColor17666 = !QTUtility.InNightMode ? ShellColors.LightModeColor : ShellColors.NightModeColor;

        public static Color NightModeOptionColor = Color.FromArgb(44, 44, 44);

        private static ShellColors.ShellColorSet colorSet = ShellColors.Create();

        public static Color Light
        {
            get
            {
                return ShellColors.colorSet.Light;
            }
        }


        public static Color Default
        {
            get
            {
                return ShellColors.colorSet.Default;
            }
        }

        public static Color ExplorerBarHrztBGColor
        {
            get
            {
                return QTUtility.LaterThan7 ?
                    SystemColors.Window :
                    Color.FromArgb(241, 245, 251);
            }
        }

        public static Color ExplorerBarVertBGColor
        {
            get
            {
                return QTUtility.LaterThan7 ?
                    SystemColors.Window :
                    Color.FromArgb(241, 245, 251);
            }
        } 

        public static Color Text {
            get
            {
                return ShellColors.colorSet.Text;
            }
        } 

        public static Color Border {
            get
            {
                return ShellColors.colorSet.Border;
            }
        } 

        public static Color Separator {
            get
            {
                return ShellColors.colorSet.Separator;
            }
        } 

        public static Color Disabled {
            get
            {
                return ShellColors.colorSet.Disabled;
            }
        } 

        public static Color Tab {
            get
            {
                return ShellColors.colorSet.Tab;
            }
        } 

        public static Color TextShadow {
            get
            {
                return ShellColors.colorSet.TextShadow;

            }
        } 

        public static void Refresh()
        {
            ShellColors.colorSet = ShellColors.Create();
        } 

        private static ShellColors.ShellColorSet Create()
        {
            if (!QTUtility.InNightMode)
                return new ShellColors.ShellColorSet();
            return QTUtility.IsWin11 ?
                new ShellColors.Windows10Dark() : 
                new ShellColors.Windows11Dark();
        }

        private class ShellColorSet
        {
          public  Color Default = Color.White;

          public  Color TreeViewBack = Color.White;

          public  Color Light = Color.FromArgb(242, 242, 242);

          public  Color Text = Color.Black;

          public  Color Border = Color.FromArgb(217, 217, 217);

          public  Color Separator = Color.FromKnownColor(KnownColor.GrayText);

          public  Color Disabled = Color.Gray;

          public  Color Tab;

          public Color TextShadow;

          public Color ViewBack;

          public Color ViewSelection;

          public  Color ViewSelectionInactive ;
          public Color ViewSelectionAndFocused;

          public Color ViewSelectionAndHilite;

          public Color ViewSelectionAndHiliteInactive;

          public Color ViewHilite;

          public Color ViewHeaderHilite;

          public Color Option;

          public  Color MenuSelection = Color.FromArgb(217, 217, 217);
        }

        private class Windows10Dark : ShellColorSet
        {
            public Windows10Dark()
            {
                Default = Color.Black;
                TreeViewBack = Color.FromArgb(25, 25, 25);
                Light = Color.FromArgb(43, 43, 43);
                Text = Color.White;
                Border = Color.FromArgb(83, 83, 83);
                Disabled = Color.FromArgb(140, 140, 140);
                Separator = Color.FromArgb(140, 140, 140);
                Tab = Color.FromArgb(217, 217, 217);
                TextShadow = Color.Gray;
                ViewBack = Color.FromArgb(32, 32, 32);
                ViewSelection = Color.FromArgb(98, 98, 98);
                ViewSelectionInactive = Color.FromArgb(51, 51, 51);
                ViewSelectionAndFocused = Color.FromArgb(119, 119, 119);
                ViewSelectionAndHilite = Color.FromArgb(119, 119, 119);
                ViewSelectionAndHiliteInactive = Color.FromArgb(119, 119, 119);
                ViewHilite = Color.FromArgb(77, 77, 77);
                ViewHeaderHilite = Color.FromArgb(67, 67, 67);
                Option = Color.FromArgb(44, 44, 44);
                MenuSelection = Color.FromArgb(65, 65, 65);
            }
        }

        private class Windows11Dark : Windows10Dark
        {
            public Windows11Dark()
            {
                Default = Color.FromArgb(30, 32, 35);
                TreeViewBack = Color.FromArgb(25, 25, 25);
                Light = Color.FromArgb(44, 44, 44);
                Border = Color.FromArgb(62, 62, 62);
                Separator = Color.FromArgb(62, 62, 62);
                Tab = Color.FromArgb(169, 169, 169);
                MenuSelection = Color.FromArgb(51, 51, 51);
            }
        }
    }



}
