using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Teardown_unlocker
{
    public partial class Form1 : Form
    {
        private Settings settings = null;

        public Form1()
        {
            InitializeComponent();
            SetProgramName();
            ShowWarningMessage();
            SetUpdateInterfaceSettings();
        }

        private void ShowWarningMessage()
        {
            var warningMessage = MessageBox.Show("\"Teardown unlocker\" makes changes to the save files. The program creates a backup, but you can still lose the current progress and statistics. The author is not responsible for the lost data!", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (warningMessage == DialogResult.No)
                this.Close();
        }

        private void SetProgramName()
        {
            this.Text = "TD Unlocker " + Settings.GetProgramVersion();
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
                    if (SettingsFileExists(Settings.GetSettingsFileName()))
                    {
                        if (CheckCorrectDataInSettings())
                        {
                            EnableInterface();
                            SetInformationInSettings();
                            ManageBackupButtonCondition();
                        }     
                        else
                            DisableInterface("Incorrect settings data");
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
            if (File.Exists(ReadSettings(Settings.GetSettingsFileName()) + @"\teardown.exe"))
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
            if (File.Exists(settings.GetTeardownSavesPath() + "savegameBackupByUnlocker.xml"))
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

        private void SetInformationInSettings()
        {
            if (settings == null)
                settings = new Settings(ReadSettings(Settings.GetSettingsFileName()), GetTeardownSavesPath());
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
            if (!File.Exists(settings.GetTeardownSavesPath() + "savegameBackupByUnlocker.xml"))
                File.Copy(settings.GetTeardownSavesPath() + "savegame.xml", settings.GetTeardownSavesPath() + "savegameBackupByUnlocker.xml");
        }

        private void UnlockSandbox(object sender, EventArgs e)
        {
            try
            {
                BackUpSave();
                WriteDataToXml(settings.GetTeardownSavesPath() + "savegame.xml", FillXmlDataWithCustomMissions());
                ReplaceSigns(settings.GetTeardownSavesPath() + "savegame.xml");
                MessageBox.Show("All sandbox level unlocked!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private XmlData FillXmlDataWithCustomMissions()
        {
            XmlData xmlData = new XmlData();
            XmlDocument doc = new XmlDocument();
            doc.Load(settings.GetTeardownSavesPath() + "savegame.xml");
            xmlData.tools = RestoreUpgradeForTools(GetToolsFromSave(doc));
            xmlData.stats = GetStatsFromSave(doc);
            xmlData.cash = GetCashFromSave(doc);
            StreamReader missions = new StreamReader(settings.GetTeardownFolderPath() + "\\data\\missions.lua");
            StreamReader messages = new StreamReader(settings.GetTeardownFolderPath() + "\\data\\messages.lua");
            xmlData.missions = GetAllMissions(missions).ToString();
            xmlData.messages = GetAllMessages(messages).ToString();
            return xmlData;
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

        //TODO: Move settings file to appdata folder
        private void SaveFolderPathInFile(String folderPath)
        {
            using (XmlWriter writer = XmlWriter.Create(Settings.GetSettingsFileName()))
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
                WriteDataToXml(settings.GetTeardownSavesPath() + "savegame.xml", FillXmlDataWithCustomCash());
                ReplaceSigns(settings.GetTeardownSavesPath() + "savegame.xml");
                MessageBox.Show("Infinity money has been added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private XmlData FillXmlDataWithCustomCash()
        {
            XmlData xmlData = new XmlData();
            XmlDocument doc = new XmlDocument();
            doc.Load(settings.GetTeardownSavesPath() + "savegame.xml");
            xmlData.tools = RestoreUpgradeForTools(GetToolsFromSave(doc));
            xmlData.stats = GetStatsFromSave(doc);
            xmlData.cash = "<cash value=\"999999\" />";
            xmlData.missions = GetMissionsFromSave(doc);
            xmlData.messages = GetMessagesFromSave(doc);
            return xmlData;
        }

        private void WriteDataToXml(string filename, XmlData xmlData)
        {
            using (XmlWriter writer = XmlWriter.Create(filename))
            {
                writer.WriteStartElement("registry");
                writer.WriteStartElement("savegame");

                writer.WriteStartElement("tool");
                writer.WriteString(xmlData.tools);
                writer.WriteEndElement();

                writer.WriteStartElement("message");
                writer.WriteString(xmlData.messages);
                writer.WriteEndElement();

                writer.WriteStartElement("mission");
                writer.WriteString(xmlData.missions);
                writer.WriteEndElement();

                writer.WriteStartElement("stats");
                writer.WriteString(xmlData.stats);
                writer.WriteEndElement();

                writer.WriteString(xmlData.cash);

                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.Flush();
            }
        }

        private void restoreWeaponsButton_Click(object sender, EventArgs e)
        {
            try
            {
                BackUpSave();                
                WriteDataToXml(settings.GetTeardownSavesPath() + "savegame.xml", FillXmlDataWithRestoredWeapons());
                ReplaceSigns(settings.GetTeardownSavesPath() + "savegame.xml");
                MessageBox.Show("Weapons upgrades has been restored!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private XmlData FillXmlDataWithRestoredWeapons()
        {
            XmlData xmlData = new XmlData();
            XmlDocument doc = new XmlDocument();
            doc.Load(settings.GetTeardownSavesPath() + "savegame.xml");
            xmlData.tools = RestoreUpgradeForTools(GetToolsFromSave(doc));
            xmlData.stats = GetStatsFromSave(doc);
            xmlData.cash = GetCashFromSave(doc);
            xmlData.missions = GetMissionsFromSave(doc);
            xmlData.messages = GetMessagesFromSave(doc);
            return xmlData;
        }

        private string RestoreUpgradeForTools(string tools)
        {
            var stringArray = tools.Split('<');
            StringBuilder result = new StringBuilder();
            foreach (var element in stringArray)
            {
                if (!element.Contains("enabled") && !element.Contains("ammo") && !element.Contains("power") && !element.Contains("damage") && !element.Contains("/") && element.Length != 0)
                    result.Append("<" + element.Substring(0, element.Length - 1) + ">\r\n\t\t\t\t<enabled value=\"1\"/>\r\n\t\t\t</" + element.Substring(0, element.Length - 1) + ">");
            }
            return result.ToString();
        }

        private void RestoreBackupButton_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(settings.GetTeardownSavesPath() + "savegame.xml");
                File.Move(settings.GetTeardownSavesPath() + "savegameBackupByUnlocker.xml", settings.GetTeardownSavesPath() + "savegame.xml");
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

        private void donateButton_Click(object sender, EventArgs e)
        {
            Process.Start("http://donationalerts.com/r/syjoosy");
        }
    }

    public struct XmlData
    {
        public string tools;
        public string messages;
        public string missions;
        public string stats;
        public string cash;
    }

    public class Settings
    {
        private const string programVersion = "v0.1.1";
        private const string settingsFileName = "settings.xml";
        private readonly string teardownFolderPath = String.Empty;
        private readonly string teardownSavesPath = String.Empty;

        public Settings(string teardownFolderPath, string teardownSavesPath)
        {
            this.teardownFolderPath = teardownFolderPath;
            this.teardownSavesPath = teardownSavesPath;
        }

        public static string GetProgramVersion()
        {
            return Settings.programVersion;
        }

        public static string GetSettingsFileName()
        {
            return Settings.settingsFileName;
        }

        public string GetTeardownFolderPath()
        {
            return this.teardownFolderPath;
        }

        public string GetTeardownSavesPath()
        {
            return this.teardownSavesPath;
        }
    }
}
