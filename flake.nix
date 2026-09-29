{
  description = "Ambiente de desenvolvimento do jogo de economia circular";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs = { nixpkgs, ... }:
    let
      supportedSystems = [ "x86_64-linux" ];
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    in
    {
      devShells = forAllSystems (system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.mkShell {
            packages = [
              pkgs.godot_4-mono
              pkgs.dotnet-sdk_8
              pkgs.sqlite
            ];

            shellHook = ''
              export DOTNET_CLI_TELEMETRY_OPTOUT=1
            '';
          };
        });
    };
}
