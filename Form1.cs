using System;
using System.Text;
using System.Windows.Forms;
using MyFileManager;
using WodToolKit.Json;

namespace Json_MOD_Tool
{
    public partial class Form_主程序 : Form
    {
        // 图标工具类实例（全局复用，最后统一释放）
        private readonly GetSystemIcon _iconHelper;
        // 文件夹图标在ImageList中的索引（预加载一次，文件夹图标无需重复创建）
        private int _folderIconIndex = -1;
        public Form_主程序()
        {
            InitializeComponent();
            // 初始化图标工具类
            _iconHelper = new GetSystemIcon();
            // 配置TreeView和ImageList（适配拖拽控件）
            InitTreeViewConfig();
            // 加载根目录（程序运行目录）
            LoadRootDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        #region 适配设计器控件：配置TreeView和ImageList
        /// <summary>
        /// 配置拖拽生成的treeView和imageList
        /// </summary>
        private void InitTreeViewConfig()
        {
            // 1. 配置imageList1（拖拽生成的ImageList）
            imgList_图标.ImageSize = new Size(16, 16); // 适配TreeView小图标
            imgList_图标.ColorDepth = ColorDepth.Depth32Bit; // 高清图标显示
            // 2. 预加载文件夹图标（文件夹图标是系统标准图标，无需重复创建）
            var folderIcon = _iconHelper.GetFolderIcon(false);
            if (folderIcon != null)
            {
                imgList_图标.Images.Add(folderIcon);
                _folderIconIndex = imgList_图标.Images.Count - 1;
            }
            // 3. 配置treeView1（拖拽生成的TreeView）
            tV_当前目录.ImageList = imgList_图标; // 绑定ImageList
            tV_当前目录.ShowLines = true;
            tV_当前目录.ShowPlusMinus = true;
            tV_当前目录.ShowRootLines = true;
            tV_当前目录.Font = new Font("微软雅黑", 9F);
            // 4. 绑定TreeView事件（懒加载+选中逻辑）
            tV_当前目录.BeforeExpand += TV_当前目录_BeforeExpand;
            tV_当前目录.AfterSelect += TV_当前目录_AfterSelect;
        }
        #endregion

        #region 树形框核心逻辑（适配拖拽控件）
        /// <summary>
        /// 加载根目录到treeView1
        /// </summary>
        private void LoadRootDirectory(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show($"根目录不存在：{rootPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 清空原有节点（避免重复加载）
            tV_当前目录.Nodes.Clear();

            // 创建根节点 根节点显示完整路径
            var rootNode = new TreeNode(rootPath)
            {
                Tag = rootPath,
                ToolTipText = rootPath
            };

            // 使用预加载的文件夹图标
            if (_folderIconIndex != -1)
            {
                rootNode.ImageIndex = _folderIconIndex;
                rootNode.SelectedImageIndex = _folderIconIndex;
            }
            else
            {
                rootNode.ImageIndex = -1;
                rootNode.SelectedImageIndex = -1;
            }

            // 添加占位节点（子节点懒加载）
            rootNode.Nodes.Add(new TreeNode("加载中..."));
            // 添加根节点到拖拽生成的treeView1
            tV_当前目录.Nodes.Add(rootNode);
            // 自动展开根节点，提升体验
            rootNode.Expand();
        }
        #endregion

        #region 刷新当前目录事件
        /// <summary>
        /// 刷新根目录（抽成公共方法，供按钮和双击调用）
        /// </summary>
        private void RefreshRootDirectory()
        {
            if (tV_当前目录.Nodes.Count == 0)
                return;
            // 获取根节点的路径
            var rootNode = tV_当前目录.Nodes[0];
            var rootPath = rootNode.Tag as string;
            if (string.IsNullOrEmpty(rootPath))
                return;
            // 刷新根目录
            LoadRootDirectory(rootPath);
            MessageBox.Show("根目录已刷新完成", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region 自动加载子节点
        /// <summary>
        /// 展开节点时懒加载子节点（适配treeView）
        /// </summary>
        private void TV_当前目录_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            var parentNode = e.Node;
            if (parentNode == null || parentNode.Tag == null)
                return;

            // 校验：已加载/无路径 → 直接返回
            if (parentNode.Nodes.Count > 0 && parentNode.Nodes[0].Text != "加载中...")
                return;

            // 清空占位节点
            parentNode.Nodes.Clear();

            // 安全地把 Tag 作为 string 取出并校验
            var parentPath = parentNode.Tag as string;
            if (string.IsNullOrEmpty(parentPath))
                return;
            try
            {
                // 1. 加载子文件夹
                foreach (var dirPath in Directory.GetDirectories(parentPath))
                {
                    var dirNode = new TreeNode(Path.GetFileName(dirPath))
                    {
                        Tag = dirPath,
                        ToolTipText = dirPath
                    };

                    // 使用预加载的文件夹图标
                    if (_folderIconIndex != -1)
                    {
                        dirNode.ImageIndex = _folderIconIndex;
                        dirNode.SelectedImageIndex = _folderIconIndex;
                    }
                    else
                    {
                        dirNode.ImageIndex = -1;
                        dirNode.SelectedImageIndex = -1;
                    }

                    // 添加占位节点（子文件夹懒加载）
                    dirNode.Nodes.Add(new TreeNode("加载中..."));
                    parentNode.Nodes.Add(dirNode);
                }
                // 2.加载图标文件
                foreach (var filePath in Directory.GetFiles(parentPath))
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileNode = new TreeNode(fileName)
                    {
                        Tag = filePath,
                        ToolTipText = filePath
                    };

                    // 每次都重新创建文件图标
                    var fileIcon = _iconHelper.GetIconByFileName(filePath, false);
                    if (fileIcon != null)
                    {
                        // 每次都添加到ImageList（即使扩展名相同）
                        imgList_图标.Images.Add(fileIcon);
                        int iconIndex = imgList_图标.Images.Count - 1;
                        fileNode.ImageIndex = iconIndex;
                        fileNode.SelectedImageIndex = iconIndex;
                    }
                    else
                    {
                        // 图标获取失败：不显示图标
                        fileNode.ImageIndex = -1;
                        fileNode.SelectedImageIndex = -1;
                    }
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                var errorNode = new TreeNode("【权限不足，无法访问】") { ForeColor = Color.Red };
                parentNode.Nodes.Add(errorNode);
            }
            catch (IOException ex)
            {
                var errorNode = new TreeNode($"【IO错误：{ex.Message}】") { ForeColor = Color.Red };
                parentNode.Nodes.Add(errorNode);
            }
        }
        #endregion

        #region 选中文件后 窗口标题显示当前选中的文件名
        /// <summary>
        /// 选中节点事件：更新窗口标题（适配treeView）
        /// </summary>
        private void TV_当前目录_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e?.Node == null)
            {
                // 无选中节点时，显示默认标题
                this.Text = "Json MOD Tool";
                return;
            }
            // 1. 获取选中节点的完整路径（从Tag中）
            string fullPath = e.Node.Tag?.ToString() ?? string.Empty;
            // 2. 提取文件名/文件夹名（核心逻辑）
            string displayName;
            if (string.IsNullOrEmpty(fullPath))
            {
                // 路径为空时，直接显示节点文本
                displayName = e.Node.Text;
            }
            else if (Directory.Exists(fullPath))
            {
                // 是文件夹：提取文件夹名
                displayName = new DirectoryInfo(fullPath).Name;
            }
            else if (File.Exists(fullPath))
            {
                // 是文件：提取文件名（含扩展名）
                displayName = Path.GetFileName(fullPath);
                if (Path.GetExtension(fullPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // 加载 JSON内容 到 tV_js内容控件
                    LoadJsonToTreeViewWithWodToolkit(fullPath);
                }
            }
            else
            {
                // 路径无效时，显示节点文本
                displayName = e.Node.Text;
            }
            // 3. 更新窗口标题（只显示文件名/文件夹名）
            this.Text = $"选择：{displayName}";
        }
        #endregion

        #region 加载 JSON 文件内容到 TreeView
        /// <summary>
        /// 核心方法：使用WodToolkit读取并解析JSON到tV_js内容
        /// </summary>
        /// <param name="jsonFilePath">JSON文件路径</param>
        private void LoadJsonToTreeViewWithWodToolkit(string jsonFilePath)
        {
            try
            {
                // 清空所有内容
                lisVie_Js内容.Items.Clear();
                // 1. 读取JSON文件内容（保持UTF-8编码）
                string jsonContent = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    // 添加空提示行
                    MessageBox.Show("无法读取 Json 文件");
                    return;
                }
                // 2. 使用WodToolkit解析JSON WodToolkit的JsonHelper是核心解析类，支持解析为dynamic/字典/对象
                dynamic result = EasyJson.ParseJsonToDynamic(jsonContent);
                if (result == null)
                {
                    MessageBox.Show("Json 文件内容为空");
                    return;
                }
                // 3. 精准获取Coordinate数组（核心适配你的固定格式）
                if (result is not IDictionary<string, object> rootDict ||
                    !rootDict.TryGetValue("Coordinate", out object? value) ||
                    value is not IList<object> coordinateList)
                {
                    MessageBox.Show("JSON文件格式错误：缺少Coordinate元素", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 4. 遍历Coordinate数组，写入ListView
                int rowNum = 1; // 编号从1开始
                foreach (var item in coordinateList)
                {
                    if (item is IDictionary<string, object> itemDict)
                    {
                        // 提取固定字段（name/x/y/z），空值兜底
                        string name = itemDict.TryGetValue("name", out object? value1) ? value1?.ToString() ?? "" : "";
                        string x = itemDict.TryGetValue("x", out object? value2) ? value2?.ToString() ?? "" : "";
                        string y = itemDict.TryGetValue("y", out object? value3) ? value3?.ToString() ?? "" : "";
                        string z = itemDict.TryGetValue("z", out object? value4) ? value4?.ToString() ?? "" : "";
                        // 添加到ListView
                        AddListViewItem(lisVie_Js内容, rowNum.ToString(), name, x, y, z);
                        rowNum++;
                    }
                }
                // 无数据时添加提示
                if (lisVie_Js内容.Items.Count == 0)
                {
                    MessageBox.Show("JSON文件中Coordinate下元素为空或无符合格式的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                // 5. 自动调整列宽（适配内容）
                SetColumnAutoWidth(lisVie_Js内容, 2); // x
                SetColumnAutoWidth(lisVie_Js内容, 3); // y
                SetColumnAutoWidth(lisVie_Js内容, 4); // z
            }
            catch (IOException ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        /// <summary>
        /// 写入单条JSON数据到ListView
        /// </summary>
        /// <param name="jsonItem">单条JSON数据</param>
        /// <param name="rowIndex">行编号（引用传递，自动递增）</param>
        private void WriteJsonItemToListView(dynamic jsonItem, ref int rowIndex)
        {
            if (jsonItem is not IDictionary<string, object> itemDict)
            {
                return;
            }
            // 提取字段（可根据你的JSON实际字段名调整，比如"name"→"名称"）
            string 名称 = itemDict.ContainsKey("name") ? itemDict["名称"]?.ToString() ?? "" : "";
            string x = itemDict.TryGetValue("x", out object? value) ? value?.ToString() ?? "" : "";
            string y = itemDict.TryGetValue("y", out object? value1) ? value1?.ToString() ?? "" : "";
            string z = itemDict.TryGetValue("z", out object? value2) ? value2?.ToString() ?? "" : "";
            // 写入ListView（编号自动递增）
            AddListViewItem(lisVie_Js内容, rowIndex.ToString(), 名称, x, y, z);
            rowIndex++;
        }
        /// <summary>
        /// 封装添加ListView行的方法
        /// </summary>
        /// <param name="listView">目标ListView</param>
        /// <param name="编号">第一列</param>
        /// <param name="名称">第二列</param>
        /// <param name="x">第三列</param>
        /// <param name="y">第四列</param>
        /// <param name="z">第五列</param>
        /// <param name="文字颜色">可选，默认黑色</param>
        private static void AddListViewItem(ListView listView, string 编号, string 名称, string x, string y, string z, Color? 文字颜色 = null)
        {
            // 创建行（第一列是编号）
            ListViewItem item = new (编号);
            // 添加其他列
            item.SubItems.Add(名称);
            item.SubItems.Add(x);
            item.SubItems.Add(y);
            item.SubItems.Add(z);
            // 设置文字颜色
            if (文字颜色.HasValue) item.ForeColor = 文字颜色.Value;
            // 添加到ListView
            listView.Items.Add(item);
        }
        
        #region 自动列宽
        /// <summary>
        /// 设置ListView指定列自动适配内容宽度
        /// </summary>
        /// <param name="listView">目标ListView</param>
        /// <param name="columnIndex">列索引</param>
        private static void SetColumnAutoWidth(ListView listView, int columnIndex)
        {
            if (listView.Columns.Count <= columnIndex)
            {
                return;
            }
            // 自动适配内容
            listView.Columns[columnIndex].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            // 限制最小宽度（避免列太窄）
            if (listView.Columns[columnIndex].Width < 60)
            {
                listView.Columns[columnIndex].Width = 60;
            }
        }
        #endregion
        #endregion
        #region 双击事件(双击根节点刷新)
        /// <summary>
        /// 新增：双击节点事件，双击根节点时刷新
        /// </summary>
        private void TV_当前目录_DoubleClick(object sender, EventArgs e)
        {
            // 1. 从sender获取TreeView控件
            if (sender is not TreeView treeView) return;
            // 2. 获取当前选中的节点（双击节点时，节点会被自动选中）
            TreeNode selectedNode = treeView.SelectedNode;
            if (selectedNode == null) return;
            // 3. 判断是否是根节点（Level=0）
            if (selectedNode.Level == 0)
            {
                RefreshRootDirectory(); // 调用你的刷新方法
            }
        }
        #endregion

        #region 点击事件(退出)
        /// <summary>
        /// 退出按钮事件：点击后关闭窗口（适配拖拽生成的button1）
        /// </summary>
        private void Bt_退出_Click(object sender, EventArgs e)
        {
            Application.Exit(); // 退出WinForm
        }
        #endregion

    }
}