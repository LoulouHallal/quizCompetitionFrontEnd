namespace PowerPointAddIn1
{
    partial class MainRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public MainRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Quiz = this.Factory.CreateRibbonTab();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.QuizTools = this.Factory.CreateRibbonGroup();
            this.button1 = this.Factory.CreateRibbonButton();
            this.button2 = this.Factory.CreateRibbonButton();
            this.Quiz.SuspendLayout();
            this.QuizTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // Quiz
            // 
            this.Quiz.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.Quiz.Groups.Add(this.group1);
            this.Quiz.Groups.Add(this.QuizTools);
            this.Quiz.Label = "Quiz";
            this.Quiz.Name = "Quiz";
            // 
            // group1
            // 
            this.group1.Label = "group1";
            this.group1.Name = "group1";
            // 
            // QuizTools
            // 
            this.QuizTools.Items.Add(this.button1);
            this.QuizTools.Items.Add(this.button2);
            this.QuizTools.Label = "Quiz tools";
            this.QuizTools.Name = "QuizTools";
            // 
            // button1
            // 
            this.button1.ImageName = "start quiz btn";
            this.button1.Label = "Start quiz";
            this.button1.Name = "button1";
            this.button1.ScreenTip = "Launches an interactive quiz for the audience";
            this.button1.ShowImage = true;
            this.button1.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Label = "Create Questions";
            this.button2.Name = "button2";
            this.button2.ScreenTip = "Create quiz questions before starting session";
            this.button2.ShowImage = true;
            this.button2.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.button2_Click);
            // 
            // MainRibbon
            // 
            this.Name = "MainRibbon";
            this.RibbonType = "Microsoft.PowerPoint.Presentation";
            this.Tabs.Add(this.Quiz);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon1_Load);
            this.Quiz.ResumeLayout(false);
            this.Quiz.PerformLayout();
            this.QuizTools.ResumeLayout(false);
            this.QuizTools.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab Quiz;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup QuizTools;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton button2;
    }

    partial class ThisRibbonCollection
    {
        internal MainRibbon Ribbon1
        {
            get { return this.GetRibbon<MainRibbon>(); }
        }
    }
}
