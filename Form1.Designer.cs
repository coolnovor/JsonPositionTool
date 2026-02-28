namespace Json_MOD_Tool
{
    partial class Form_主程序
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_主程序));
            bt_退出 = new Button();
            tV_当前目录 = new TreeView();
            imgList_图标 = new ImageList(components);
            lisVie_Js内容 = new ListView();
            编号 = new ColumnHeader();
            名称 = new ColumnHeader();
            x = new ColumnHeader();
            y = new ColumnHeader();
            z = new ColumnHeader();
            SuspendLayout();
            // 
            // bt_退出
            // 
            resources.ApplyResources(bt_退出, "bt_退出");
            bt_退出.Name = "bt_退出";
            bt_退出.UseVisualStyleBackColor = true;
            bt_退出.Click += Bt_退出_Click;
            // 
            // tV_当前目录
            // 
            resources.ApplyResources(tV_当前目录, "tV_当前目录");
            tV_当前目录.Name = "tV_当前目录";
            tV_当前目录.BeforeExpand += TV_当前目录_BeforeExpand;
            tV_当前目录.AfterSelect += TV_当前目录_AfterSelect;
            tV_当前目录.DoubleClick += TV_当前目录_DoubleClick;
            // 
            // imgList_图标
            // 
            imgList_图标.ColorDepth = ColorDepth.Depth32Bit;
            resources.ApplyResources(imgList_图标, "imgList_图标");
            imgList_图标.TransparentColor = Color.Transparent;
            // 
            // lisVie_Js内容
            // 
            lisVie_Js内容.Columns.AddRange(new ColumnHeader[] { 编号, 名称, x, y, z });
            lisVie_Js内容.FullRowSelect = true;
            lisVie_Js内容.HideSelection = true;
            resources.ApplyResources(lisVie_Js内容, "lisVie_Js内容");
            lisVie_Js内容.Name = "lisVie_Js内容";
            lisVie_Js内容.UseCompatibleStateImageBehavior = false;
            lisVie_Js内容.View = View.Details;
            // 
            // 编号
            // 
            resources.ApplyResources(编号, "编号");
            // 
            // 名称
            // 
            resources.ApplyResources(名称, "名称");
            // 
            // x
            // 
            resources.ApplyResources(x, "x");
            // 
            // y
            // 
            resources.ApplyResources(y, "y");
            // 
            // z
            // 
            resources.ApplyResources(z, "z");
            // 
            // Form_主程序
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lisVie_Js内容);
            Controls.Add(tV_当前目录);
            Controls.Add(bt_退出);
            Name = "Form_主程序";
            ResumeLayout(false);
        }

        #endregion

        private Button bt_退出;
        private TreeView tV_当前目录;
        private ImageList imgList_图标;
        private ListView lisVie_Js内容;
        private ColumnHeader 编号;
        private ColumnHeader 名称;
        private ColumnHeader x;
        private ColumnHeader y;
        private ColumnHeader z;
    }
}
