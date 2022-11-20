using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Teardown_unlocker
{
    public partial class Form1 : Form
    {
        const string settingsFileName = "settings.xml";
        string teardownFolderPath = String.Empty;
        string teardownSavesPath = String.Empty;

        public Form1()
        {
            InitializeComponent();
            SetUpdateInterfaceSettings();
        }

        private void SetUpdateInterfaceSettings()
        {
            Timer timer = new Timer();
            timer.Interval = (10 * 100); // 1 sec
            timer.Tick += new EventHandler(UpdateInterface);
            timer.Start();
        }

        private void UpdateInterface(object sender, EventArgs e)
        {
            try
            {
                if (CheckTeardownProccesRun())
                {
                    if (SettingsFileExists(settingsFileName))
                    {
                        if (CheckCorrectDataInSettings())
                        {
                            EnableInterface();
                            if (String.IsNullOrEmpty(teardownFolderPath))
                                teardownFolderPath = ReadSettings(settingsFileName);
                            if (String.IsNullOrEmpty(teardownSavesPath))
                                teardownSavesPath = GetTeardownSavesPath();

                            ManageBackupButtonCondition();
                        }     
                        else
                            DisableInterface("Incorrect data");
                    }
                    else
                        DisableInterface("Choose teardown folder");
                }
                else
                    DisableInterface("Close teardown");
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CheckTeardownProccesRun()
        {
            if (Process.GetProcessesByName("teardown").Length == 0)
                return true;
            else
                return false;
        }

        private bool CheckCorrectDataInSettings()
        {
            if (File.Exists(ReadSettings(settingsFileName) + @"\teardown.exe"))
                return true;
            else
                return false;
        }

        private void ManageBackupButtonCondition()
        {
            if (CheckSaveBackupExists())
                restoreBackupButton.Enabled = true;
            else
                restoreBackupButton.Enabled = false;
        }

        private bool CheckSaveBackupExists()
        {
            if (File.Exists(teardownSavesPath + "savegameBackupByUnlocker.xml"))
                return true;
            else
                return false;
        }

        private void DisableInterface(string errorMessage)
        {
            toolStripStatusLabel2.ForeColor = Color.Red;
            toolStripStatusLabel2.Text = errorMessage;
            unlockSandboxButton.Enabled = false;
            infinityCashButton.Enabled = false;
            restoreWeaponsButton.Enabled = false;
            restoreBackupButton.Enabled = false;
        }

        private void EnableInterface()
        {
            toolStripStatusLabel2.ForeColor = Color.LimeGreen;
            toolStripStatusLabel2.Text = "Ready";
            unlockSandboxButton.Enabled = true;
            infinityCashButton.Enabled = true;
            restoreWeaponsButton.Enabled = true;
            restoreBackupButton.Enabled = false;
        }

        private string GetTeardownSavesPath()
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Teardown\" + "savegame.xml"))
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Teardown\";
            else if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Teardown\" + "savegame.xml"))
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Teardown\";
            else
                throw new Exception("Teardown saves folder not found!");
        }

        private bool SettingsFileExists(string settings)
        {
            if (File.Exists(settings))
                return true;
            else
                return false;
        }

        private string GetToolsFromSave(XmlDocument doc)
        {
            XmlNode node = doc.DocumentElement.SelectSingleNode("/registry/savegame/tool");
            return node.InnerXml;
        }

        private string GetStatsFromSave(XmlDocument doc)
        {
            XmlNode node = doc.DocumentElement.SelectSingleNode("/registry/savegame/stats");
            return node.InnerXml;
        }

        private string GetMissionsFromSave(XmlDocument doc)
        {
            XmlNode node = doc.DocumentElement.SelectSingleNode("/registry/savegame/mission");
            return node.InnerXml;
        }

        private string GetMessagesFromSave(XmlDocument doc)
        {
            XmlNode node = doc.DocumentElement.SelectSingleNode("/registry/savegame/message");
            return node.InnerXml;
        }

        private string GetCashFromSave(XmlDocument doc)
        {
            XmlNode node = doc.DocumentElement.SelectSingleNode("/registry/savegame/cash");
            return node.OuterXml;
        }

        private void BackUpSave()
        {
            if (!File.Exists(teardownSavesPath + "savegameBackupByUnlocker.xml"))
                File.Copy(teardownSavesPath + "savegame.xml", teardownSavesPath + "savegameBackupByUnlocker.xml");
        }

        private void UnlockSandbox(object sender, EventArgs e)
        {
            try
            {
                BackUpSave();
                XmlDocument doc = new XmlDocument();
                doc.Load(teardownSavesPath + "savegame.xml");
                var tools = GetToolsFromSave(doc);
                var brokenVoxels = GetStatsFromSave(doc);
                var cash = GetCashFromSave(doc);

                var filename = teardownSavesPath + "savegame.xml";

                StreamReader missions = new StreamReader(teardownFolderPath + "\\data\\missions.lua");
                var allMissions = GetAllMissions(missions);
                var messages = new StreamReader(teardownFolderPath + "\\data\\messages.lua");
                var allMessages = GetAllMessages(messages);

                using (XmlWriter writer = XmlWriter.Create(filename))
                {
                    writer.WriteStartElement("registry");
                    writer.WriteStartElement("savegame");

                    writer.WriteStartElement("tool");
                    writer.WriteString(tools);
                    writer.WriteEndElement();

                    writer.WriteStartElement("message");
                    writer.WriteString(allMessages.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("mission");
                    writer.WriteString(allMissions.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("stats");
                    writer.WriteString(brokenVoxels);
                    writer.WriteEndElement();

                    writer.WriteString(cash);

                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.Flush();
                }
                ReplaceSigns(filename);
                MessageBox.Show("All sandbox level unlocked!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void ReplaceSigns(string filename)
        {
            string text = File.ReadAllText(filename);
            text = text.Replace("&lt;", "<").Replace("&gt;", ">");
            File.WriteAllText(filename, text);
        }

        private StringBuilder GetAllMissions(StreamReader missions)
        {
            var allMissions = new StringBuilder();
            while (!missions.EndOfStream)
            {
                string line = missions.ReadLine();
                if (line.StartsWith("gMissions["))
                {
                    var temp = line.Split('\"');
                    allMissions.Append("<" + temp[1] + " value=\"1\">\r\n\t\t\t\t<score value=\"3\"/>\r\n\t\t\t\t<timeleft value=\"-1\"/>\r\n\t\t\t\t<missiontime value=\"2\"/>\r\n\t\t\t</" + temp[1] + ">");
                }
            }
            missions.Close();
            return allMissions;
        }

        private StringBuilder GetAllMessages(StreamReader messages)
        {
            var allMessages = new StringBuilder();
            while (!messages.EndOfStream)
            {
                string line = messages.ReadLine();
                if (line.StartsWith("gMessages["))
                {
                    var temp = line.Split('\"');
                    allMessages.Append("<" + temp[1] + " value=\"2\"/>");
                }
            }
            messages.Close();
            return allMessages;
        }

        private void ChooseTeardownFolder(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
                if (CheckFolderIsCorrect(folderBrowser.SelectedPath))
                    SaveFolderPathInFile(folderBrowser.SelectedPath);
                else
                    MessageBox.Show("Teardown.exe file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        private void SaveFolderPathInFile(String folderPath)
        {
            using (XmlWriter writer = XmlWriter.Create(settingsFileName))
            {
                writer.WriteStartElement("settings");
                writer.WriteElementString("TeardownFolderPath", folderPath);
                writer.WriteEndElement();
                writer.Flush();
            }
        }

        private string ReadSettings(string settings)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(settings);
            XmlNode node = doc.DocumentElement.SelectSingleNode("/settings/TeardownFolderPath");
            return node.InnerText;
        }

        private bool CheckFolderIsCorrect(String folderPath)
        {
            if (File.Exists(folderPath + @"\Teardown.exe"))
                return true;
            else
                return false;
        }

        private void UpgradeCashFromSave(object sender, EventArgs e)
        {
            try
            {
                BackUpSave();
                XmlDocument doc = new XmlDocument();
                doc.Load(teardownSavesPath + "savegame.xml");
                var tools = GetToolsFromSave(doc);
                var brokenVoxels = GetStatsFromSave(doc);
                var cash = "<cash value=\"999999\" />";
                var missions = GetMissionsFromSave(doc);
                var messages = GetMessagesFromSave(doc);
                var filename = teardownSavesPath + "savegame.xml";

                using (XmlWriter writer = XmlWriter.Create(filename))
                {
                    writer.WriteStartElement("registry");
                    writer.WriteStartElement("savegame");

                    writer.WriteStartElement("tool");
                    writer.WriteString(tools);
                    writer.WriteEndElement();

                    writer.WriteStartElement("message");
                    writer.WriteString(missions);
                    writer.WriteEndElement();

                    writer.WriteStartElement("mission");
                    writer.WriteString(messages);
                    writer.WriteEndElement();

                    writer.WriteStartElement("stats");
                    writer.WriteString(brokenVoxels);
                    writer.WriteEndElement();

                    writer.WriteString(cash);

                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.Flush();
                }
                ReplaceSigns(filename);
                MessageBox.Show("Infinity money has been added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void restoreWeaponsButton_Click(object sender, EventArgs e)
        {
            try
            {
                BackUpSave();
                XmlDocument doc = new XmlDocument();
                doc.Load(teardownSavesPath + "savegame.xml");
                var tools = RestoreUpgradeForTools(GetToolsFromSave(doc));
                var brokenVoxels = GetStatsFromSave(doc);
                var cash = GetCashFromSave(doc);
                var missions = GetMissionsFromSave(doc);
                var messages = GetMessagesFromSave(doc);
                var filename = teardownSavesPath + "savegame.xml";

                using (XmlWriter writer = XmlWriter.Create(filename))
                {
                    writer.WriteStartElement("registry");
                    writer.WriteStartElement("savegame");

                    writer.WriteStartElement("tool");
                    writer.WriteString(tools);
                    writer.WriteEndElement();

                    writer.WriteStartElement("message");
                    writer.WriteString(missions);
                    writer.WriteEndElement();

                    writer.WriteStartElement("mission");
                    writer.WriteString(messages);
                    writer.WriteEndElement();

                    writer.WriteStartElement("stats");
                    writer.WriteString(brokenVoxels);
                    writer.WriteEndElement();

                    writer.WriteString(cash);

                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    writer.Flush();
                }
                ReplaceSigns(filename);
                MessageBox.Show("Weapons upgrades has been restored!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private string RestoreUpgradeForTools(string tools)
        {
            var s = tools.Split('<');
            StringBuilder shit = new StringBuilder();
            foreach (var a in s)
            {
                if (!a.Contains("enabled") && !a.Contains("ammo") && !a.Contains("power") && !a.Contains("damage") && !a.Contains("/") && a.Length != 0)
                    shit.Append("<" + a.Substring(0, a.Length - 1) + ">\r\n\t\t\t\t<enabled value=\"1\"/>\r\n\t\t\t</" + a.Substring(0, a.Length - 1) + ">");
            }
            return shit.ToString();
        }

        private void RestoreBackupButton_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(teardownSavesPath + "savegame.xml");
                File.Move(teardownSavesPath + "savegameBackupByUnlocker.xml", teardownSavesPath + "savegame.xml");
                MessageBox.Show("Data restored from backup!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://github.com/syjoosy");
            
        }
    }
}
