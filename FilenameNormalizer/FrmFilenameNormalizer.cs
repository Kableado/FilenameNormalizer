using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace FilenameNormalizer
{
    public partial class FrmFilenameNormalizer : Form
    {
        public string InitialPath
        {
            get { return txtPath.Text; }
            set { txtPath.Text = value; }
        }

        public FrmFilenameNormalizer()
        {
            InitializeComponent();
            txtPath.Text = Directory.GetCurrentDirectory();
            cbFilter_Init();
        }

        #region Filter

        private void cbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtFilter.Enabled = (cbFilter.SelectedIndex == 4);
        }

        private void cbFilter_Init()
        {
            this.cbFilter.Items.AddRange(new object[] {
                "All",
                "DSC_ + MOV_",
                "IMG_ + VID_",
                "DSC_ + MOV_ + IMG_ + VID_",
                "Custom"});
            cbFilter.SelectedIndex = 0;
            txtFilter.Enabled = false;
        }

        private bool cbFilter_ApplyFilter(string fileName)
        {
            switch(cbFilter.SelectedIndex){
                case 0:
                    return true;
                case 1:
                    return (fileName.StartsWith("DSC_") || fileName.StartsWith("MOV_"));
                case 2:
                    return (fileName.StartsWith("IMG_") || fileName.StartsWith("VID_"));
                case 3:
                    return (fileName.StartsWith("DSC_") || fileName.StartsWith("IMG_") || fileName.StartsWith("MOV_") || fileName.StartsWith("VID_"));
                case 4:
                    return txtFilter_ApplyFilter(fileName);
            }
            return false;
        }

        private bool txtFilter_ApplyFilter(string fileName)
        {
            if (string.IsNullOrEmpty(txtFilter.Text))
            {
                return false;
            }

            string[] filters = txtFilter.Text.Split('|');

            foreach (string strFilter in filters)
            {
                if (fileName.Contains(strFilter))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void lsbFiles_Clean()
        {
            lsbFiles.Items.Clear();
        }

        private void lsbFiles_AddLine(string line)
        {
            lsbFiles.Items.Add(line);

            Application.DoEvents();

            int visibleItems = lsbFiles.ClientSize.Height / lsbFiles.ItemHeight;
            lsbFiles.TopIndex = Math.Max(lsbFiles.Items.Count - visibleItems + 1, 0);
        }

        /// <summary>
        /// Returns the EXIF Image Data of the Date Taken.
        /// </summary>
        /// <param name="getImage">Image (If based on a file use Image.FromFile(f);)</param>
        /// <returns>Date Taken or Null if Unavailable</returns>
        public static DateTime? DateTaken(Image getImage)
        {
            int DateTakenValue = 0x9003; //36867;

            if (!getImage.PropertyIdList.Contains(DateTakenValue))
                return null;

            string dateTakenTag = System.Text.Encoding.ASCII.GetString(getImage.GetPropertyItem(DateTakenValue).Value);
            string[] parts = dateTakenTag.Split(':', ' ');
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);
            int day = int.Parse(parts[2]);
            int hour = int.Parse(parts[3]);
            int minute = int.Parse(parts[4]);
            int second = int.Parse(parts[5]);

            return new DateTime(year, month, day, hour, minute, second);
        }

        public static DateTime FirstDate(string filePath)
        {
            DateTime dtFile;

            string ext = Path.GetExtension(filePath).ToLower();

            DateTime dtCretation = File.GetCreationTime(filePath);
            DateTime dtLastMod = File.GetLastWriteTime(filePath);
            if (dtCretation < dtLastMod)
            {
                dtFile = dtCretation;
            }
            else
            {
                dtFile = dtLastMod;
            }

            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
            {
                Image img = Image.FromFile(filePath);
                DateTime? dtTaken = DateTaken(img);
                if (dtTaken != null)
                {
                    dtFile = (DateTime)dtTaken;
                }
                img.Dispose();
            }

            return dtFile;
        }

        private void ProcessDirectory(string path, bool doMove)
        {
            string[] filePaths = Directory.GetFiles(path);
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);

                if (cbFilter_ApplyFilter(fileName))
                {
                    DateTime dtFile = FirstDate(filePath);
                    string ext = Path.GetExtension(filePath).ToLower();
                    string fileDirPath = Path.GetDirectoryName(filePath);
                    string newFilePath = string.Format("{0}/{1}{2}{3}", fileDirPath, dtFile.ToString("yyyyMMdd-HHmmss"), txtSuffix.Text, ext);


                    if (doMove)
                    {
                        int retry = 1;
                        while (File.Exists(newFilePath))
                        {
                            newFilePath = string.Format("{0}/{1}-{2}{3}{4}", fileDirPath, dtFile.ToString("yyyyMMdd-HHmmss"), retry, txtSuffix.Text, ext);
                            retry++;
                        }
                        File.Move(filePath, newFilePath);
                    }

                    lsbFiles_AddLine(string.Format("RenameTo: {0} {1} ", fileName, newFilePath));
                }
                else
                {
                    lsbFiles_AddLine(string.Format("Unchanged: {0}", fileName));
                }
            }

            string[] directoryPaths = Directory.GetDirectories(path);
            foreach (string directoryPath in directoryPaths)
            {
                ProcessDirectory(directoryPath, doMove);
            }
        }

        private void Process(bool doMove)
        {
            string path = txtPath.Text;
            if (!Directory.Exists(path))
            {
                MessageBox.Show("El directorio no existe");
                return;
            }

            lsbFiles_Clean();
            ProcessDirectory(path, doMove);
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            Process(true);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            Process(false);
        }

    }
}
