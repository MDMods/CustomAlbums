using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using Main = CustomAlbums.Main;

[assembly: AssemblyTitle(Main.MelonName)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct(Main.MelonName)]
[assembly: AssemblyCopyright("Copyright © Muse Dash Modding Community 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("8ea4daf8-4ffd-465f-9b07-ac6925c6453f")]

[assembly: AssemblyVersion($"{Main.MelonVersion}.0")]
[assembly: AssemblyFileVersion($"{Main.MelonVersion}.0")]
[assembly: MelonInfo(typeof(Main), Main.MelonName, Main.MelonVersion, Main.MelonAuthor)]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]
[assembly: MelonColor(255, 0, 255, 150)]
