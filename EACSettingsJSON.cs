using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eac_xtract
{
    internal class EACSettingsJSON
    {
        public string? title { get; set; }
        public string? executable { get; set; }
        public string? productid { get; set; }
        public string? sandboxid { get; set; }
        public string? deploymentid { get; set; }
        public string? requested_splash { get; set; }
        public string? wait_for_game_process_exit { get; set; }
        public string? hide_bootstrapper { get; set; }
        public string? hide_gui { get; set; }
        public string? allow_null_client { get; set; }
    }
}
