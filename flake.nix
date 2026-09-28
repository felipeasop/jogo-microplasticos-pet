{
  description = "Ambiente de desenvolvimento do jogo educativo sobre microplásticos";

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
            packages = with pkgs; [
              godot_4-mono
              dotnet-sdk_8
              sqlite
            ];

            shellHook = ''
              export DOTNET_CLI_TELEMETRY_OPTOUT=1
            '';
          };
        });
    };
}
