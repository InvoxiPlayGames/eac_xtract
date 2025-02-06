using System.Text.Json;

namespace eac_xtract
{
    internal class Program
    {
        // all platforms that return a result from the CDN
        static string[] SupportedPlatformList = [
            "win64", // Windows 64-bit OS, 64-bit start_protected_game, 64-bit game
            "win64_wow64", // todo

            "linux64", // Linux 64-bit OS, 64-bit start_protected_game, 64-bit game
            "linux32", // todo
            "linux64_32", // todo
            "linux32_64", // todo

            "mac64", // macOS Intel or Rosetta 2 game
            "mac_arm64", // macOS Apple Silicon game
        ];

        // deprecated in EOS SDK 1.16
        static string[] DeprecatedPlatformList = [
            "win32",
            "wow64",
            "wow64_win64",
        ];

        // platforms people care about
        static string[] ImportantPlatformList =
        [
            "win64",
            "linux64",
            "mac64", // who invited my man :sob: blud thinks he's on the team
            "mac_arm64"
        ];

        static string GetFileExtension(string platformName)
        {
            if (platformName.StartsWith("win") || platformName.StartsWith("wow"))
                return "dll";
            if (platformName.StartsWith("mac") || platformName.StartsWith("osx"))
                return "dylib";
            if (platformName.StartsWith("linux"))
                return "so";
            return "mod";
        }

        static void PrintUsage()
        {
            Console.WriteLine("usage:");
            Console.WriteLine("  decrypting a given module:");
            Console.WriteLine("    ./eac_xtract decrypt /path/to/module /path/to/output");
            Console.WriteLine("  list all active modules for a game:");
            Console.WriteLine("    ./eac_xtract list /path/to/game/Settings.json");
            Console.WriteLine("  decrypt all active modules for a game:");
            Console.WriteLine("    ./eac_xtract all /path/to/game/Settings.json /path/to/output/folder");
            Console.WriteLine("");
            Console.WriteLine("  (a pair of productId:deploymentId can be given in place of a Settings.json path)");
        }

        static void DecryptModuleFromFile(string in_path, string out_path)
        {
            byte[] module = File.ReadAllBytes(in_path);
            byte[] decmod = EACCrypto.DecryptBuffer(module);
            File.WriteAllBytes(out_path, decmod);
        }

        static async Task<Dictionary<string, byte[]>> GetAllEnabledModules(string product_id, string deployment_id, bool include_all = false, bool include_deprecated = false)
        {
            Dictionary<string, byte[]> modules = new Dictionary<string, byte[]>();
            foreach (string platform in (include_all == false ? ImportantPlatformList : SupportedPlatformList))
            {
                byte[]? moduleData = await EACModulesCDN.DownloadModule(product_id, deployment_id, platform);
                if (moduleData != null && moduleData.Length > 2)
                    modules.Add(platform, moduleData);
            }
            if (include_deprecated)
            {
                foreach (string platform in DeprecatedPlatformList)
                {
                    byte[]? moduleData = await EACModulesCDN.DownloadModule(product_id, deployment_id, platform);
                    if (moduleData != null && moduleData.Length > 2)
                        modules.Add(platform, moduleData);
                }
            }
            return modules;
        }
        static async Task<List<string>> GetListAllEnabledModules(string product_id, string deployment_id, bool include_all = false, bool include_deprecated = false)
        {
            List<string> modules = new List<string>();
            foreach (string platform in (include_all == false ? ImportantPlatformList : SupportedPlatformList))
            {
                int moduleExists = await EACModulesCDN.DoesModuleExist(product_id, deployment_id, platform);
                if (moduleExists != 0)
                    modules.Add(platform);
            }
            if (include_deprecated)
            {
                foreach (string platform in DeprecatedPlatformList)
                {
                    int moduleExists = await EACModulesCDN.DoesModuleExist(product_id, deployment_id, platform);
                    if (moduleExists != 0)
                        modules.Add(platform);
                }
            }
            return modules;
        }

        static (string, string)? GetProductIDDeploymentID(string argument)
        {
            if (File.Exists(argument))
            {
                string jsonfile = File.ReadAllText(argument);
                EACSettingsJSON? settings = JsonSerializer.Deserialize<EACSettingsJSON>(jsonfile);
                if (settings != null)
                    return (settings.productid!, settings.deploymentid!);
            }
            else if (argument.Contains(":") && argument.Split(":")[0].Length > 3)
            {
                string[] splits = argument.Split(":");
                if (splits.Length == 2)
                    return (splits[0], splits[1]);
            }
            return null;
        }

        static async Task ListEnabledModules(string product_id, string deployment_id)
        {
            Console.WriteLine("Checking for modules...");
            List<string> module_list = await GetListAllEnabledModules(product_id, deployment_id);
            Console.WriteLine("Supported modules:");
            Console.Write("    ");
            foreach (string module in module_list)
            {
                Console.Write("{0}, ", module);
            }
            Console.WriteLine();
        }

        static async Task DownloadEnabledModules(string product_id, string deployment_id, string out_dir)
        {
            if (!Directory.Exists(out_dir))
                Directory.CreateDirectory(out_dir);

            Console.WriteLine("Downloading modules...");
            Dictionary<string, byte[]> all_modules = await GetAllEnabledModules(product_id, deployment_id);
            Console.WriteLine("Decrypting modules:");
            foreach (string module in all_modules.Keys)
            {
                string out_filename = $"{product_id}_{module}.{GetFileExtension(module)}";
                string out_path = Path.Join(out_dir, out_filename);
                byte[] decrypted_module = EACCrypto.DecryptBuffer(all_modules[module]);
                Console.WriteLine($"    {module} - {decrypted_module.Length} bytes @ {out_filename}");
                File.WriteAllBytes(out_path, decrypted_module);
            }
        }

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string verb = args[0];
            switch (verb.ToLower())
            {
                case "decrypt":
                    {
                        // check argument count first
                        if (args.Length < 3) { PrintUsage(); return; }
                        // decrypt it directly
                        DecryptModuleFromFile(args[1], args[2]);
                    }
                    break;

                case "list":
                    {
                        // check argument count and sanity first
                        if (args.Length < 2) { PrintUsage(); return; }
                        // get the ids from either a json file or a given tuple at the cli
                        (string product, string deployment)? ids = GetProductIDDeploymentID(args[1]);
                        if (ids == null) { PrintUsage(); return; }
                        // list the modules
                        await ListEnabledModules(ids.Value.product, ids.Value.deployment);
                    }
                    break;

                case "all":
                    {
                        // check argument count and sanity first
                        if (args.Length < 3) { PrintUsage(); return; }
                        // get the ids from either a json file or a given tuple at the cli
                        (string product, string deployment)? ids = GetProductIDDeploymentID(args[1]);
                        if (ids == null) { PrintUsage(); return; }
                        // decrypt the modules
                        await DownloadEnabledModules(ids.Value.product, ids.Value.deployment, args[2]);
                    }
                    break;

                default:
                    {
                        PrintUsage();
                    }
                    break;
            }
        }
    }
}
