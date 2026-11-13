using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Radeoner
{
    public sealed partial class MainForm
    {
        private readonly Button aboutProgram = Button(), supportProject = Button();
        private readonly GroupBox informationGroup = new GroupBox();
        private const string AuthorUrl = "https://proszczok.pl/", PaypalUrl = "https://paypal.me/proszczok",
            RevolutUrl = "https://revolut.me/r_rosiejka", TelegramUrl = "https://t.me/proszczok";
        private void BuildInformation()
        {
            var fields = SettingsGroup(informationGroup);
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            row.Controls.Add(aboutProgram); row.Controls.Add(supportProject); fields.Controls.Add(row);
            aboutProgram.Click += (s, e) => SystemDialogs.Show(this, "Radeoner", L.T("info.aboutTitle", L.Version), L.T("info.aboutText"), false,
                LinkAction(101, "info.website", AuthorUrl), LinkAction(102, "info.source", "https://github.com/proszczok2002/Radeoner"));
            supportProject.Click += (s, e) => SystemDialogs.Show(this, "Radeoner", L.T("info.supportTitle"), L.T("info.supportText"), false,
                LinkAction(101, "info.paypal", PaypalUrl), LinkAction(102, "info.revolut", RevolutUrl),
                LinkAction(103, "info.telegram", TelegramUrl), LinkAction(104, "info.website", AuthorUrl));
        }
        private static DialogAction LinkAction(int id, string key, string url)
        { return new DialogAction { Id = id, Text = L.T(key), Invoke = window => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }) }; }
        private void LocalizeInformation()
        {
            SetButton(aboutProgram, "info.about", "info.aboutName"); SetButton(supportProject, "info.support", "info.supportName");
            informationGroup.Text = informationGroup.AccessibleName = L.T("info.heading");
        }
    }
}
