using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace MyFileManager
{
    /// <summary>
    /// 系统图标获取工具类（自动资源管理、适配Windows原生样式）
    /// 支持：文件图标、文件夹图标、文件类型图标，无资源泄漏
    /// </summary>
    public class GetSystemIcon : IDisposable
    {
        #region 私有字段（资源追踪）
        // 追踪所有创建的Icon对象，用于最终释放（解决资源泄漏）
        private readonly List<Icon> _createdIcons = new List<Icon>();
        // 标记是否已释放资源（防止重复释放）
        private bool _disposed;
        #endregion

        #region 公开方法：获取图标
        /// <summary>
        /// 依据文件完整路径读取图标（支持大/小图标）
        /// </summary>
        /// <param name="fileName">文件完整路径</param>
        /// <param name="isLarge">是否返回大图标（32x32），默认小图标（16x16）</param>
        /// <returns>系统图标，文件不存在/获取失败返回null</returns>
        public Icon? GetIconByFileName(string fileName, bool isLarge = false)
        {
            // 空值+存在性校验
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
                return null;

            SHFILEINFO shinfo = new SHFILEINFO();
            // 组合API标志位：获取图标 + 指定尺寸
            uint flags = Win32.SHGFI_ICON | (isLarge ? Win32.SHGFI_LARGEICON : Win32.SHGFI_SMALLICON);

            // 调用Windows API获取图标句柄
            IntPtr result = Win32.SHGetFileInfo(
                fileName,
                0,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                flags);

            // 校验图标句柄是否有效
            if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                return null;

            // 创建Icon对象（由Icon托管句柄，避免手动释放的错误）
            Icon icon = Icon.FromHandle(shinfo.hIcon);
            // 加入追踪列表，用于后续自动释放
            _createdIcons.Add(icon);
            return icon;
        }

        /// <summary>
        /// 依据文件扩展名获取对应图标（非扩展名则返回文件夹图标）
        /// </summary>
        /// <param name="fileType">文件扩展名（如.txt）</param>
        /// <param name="isLarge">是否返回大图标（32x32）</param>
        /// <returns>系统图标，获取失败返回null</returns>
        public Icon? GetIconByFileType(string fileType, bool isLarge)
        {
            // 空值校验
            if (string.IsNullOrWhiteSpace(fileType))
                return null;

            // 如果是文件夹类型（不以.开头），直接返回标准文件夹图标
            if (!fileType.StartsWith("."))
                return GetFolderIcon(isLarge);

            string ?regIconString = null;
            string systemDir = Environment.SystemDirectory;

            // 注册表操作使用using，自动释放注册表句柄（解决注册表泄漏）
            using (RegistryKey ?extKey = Registry.ClassesRoot.OpenSubKey(fileType, false))
            {
                if (extKey != null)
                {
                    string ?fileTypeName = extKey.GetValue("") as string;
                    if (!string.IsNullOrWhiteSpace(fileTypeName))
                    {
                        using (RegistryKey ?iconKey = Registry.ClassesRoot.OpenSubKey($@"{fileTypeName}\DefaultIcon", false))
                        {
                            if (iconKey != null)
                                regIconString = iconKey.GetValue("") as string;
                        }
                    }
                }
            }

            // 兜底：未知文件类型图标
            regIconString ??= Path.Combine(systemDir, "shell32.dll,0");

            try
            {
                // 解析图标路径：[文件路径],[图标索引]
                string[] iconParts = regIconString.Split(',');
                if (iconParts.Length != 2)
                    iconParts = new[] { Path.Combine(systemDir, "shell32.dll"), "2" };

                string iconFile = iconParts[0];
                int iconIndex = int.Parse(iconParts[1]);

                // 调用API提取图标
                int[] largeIcons = new int[1];
                int[] smallIcons = new int[1];
                uint count = Win32.ExtractIconEx(
                    iconFile,
                    iconIndex,
                    largeIcons,
                    smallIcons,
                    1);

                if (count == 0)
                    return null;

                IntPtr iconHandle = new IntPtr(isLarge ? largeIcons[0] : smallIcons[0]);
                if (iconHandle == IntPtr.Zero)
                    return null;

                // 创建Icon对象并追踪
                Icon icon = Icon.FromHandle(iconHandle);
                _createdIcons.Add(icon);
                return icon;
            }
            catch (FormatException)
            {
                // 图标索引解析失败
                return null;
            }
            catch (IOException)
            {
                // 图标文件不存在或无法访问
                return null;
            }
        }

        /// <summary>
        /// 获取系统标准文件夹图标（100%匹配Windows原生样式）
        /// </summary>
        /// <param name="isLarge">是否返回大图标（32x32）</param>
        /// <returns>系统文件夹图标，获取失败返回null</returns>
        public Icon? GetFolderIcon(bool isLarge)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            // 组合标志位：获取文件夹图标 + 指定尺寸
            uint flags = Win32.SHGFI_ICON | Win32.SHGFI_FOLDER | (isLarge ? Win32.SHGFI_LARGEICON : Win32.SHGFI_SMALLICON);

            // 调用API获取标准文件夹图标（空路径+目录属性）
            IntPtr result = Win32.SHGetFileInfo(
                "",
                0x10, // FILE_ATTRIBUTE_DIRECTORY（目录属性）
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                flags);

            if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                return null;

            Icon icon = Icon.FromHandle(shinfo.hIcon);
            _createdIcons.Add(icon);
            return icon;
        }
        #endregion

        #region 资源释放（自动释放Icon资源，解决泄漏）
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放所有创建的Icon对象
                foreach (Icon icon in _createdIcons)
                {
                    try
                    {
                        icon.Dispose();
                    }
                    catch { }
                }
                _createdIcons.Clear();
            }

            _disposed = true;
        }

        ~GetSystemIcon()
        {
            Dispose(false);
        }
        #endregion
    }

    #region 辅助结构体和Win32 API声明
    /// <summary>
    /// 存储SHGetFileInfo API返回的文件信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    /// <summary>
    /// Windows API声明（仅内部访问）
    /// </summary>
    internal static class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_FOLDER = 0x00000020; // 获取文件夹图标的标志位

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            int[] phiconLarge,
            int[] phiconSmall,
            uint nIcons);
    }
    #endregion
}