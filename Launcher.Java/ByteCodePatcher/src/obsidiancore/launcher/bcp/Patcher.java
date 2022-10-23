package obsidiancore.launcher.bcp;

import java.io.File;

public class Patcher {
    public static void main(String[] args) throws Exception {
        if (args.length != 2) System.exit(1);
        File libFile = new File(args[1]).getCanonicalFile();
        if (!libFile.isFile()) System.exit(1);

        try {
            if ("CoreModManager_Sort_Patch".equals(args[0]))
                CoreModManager_Sort_Patch.patchSort(libFile);
            else if ("SecureJarHandler_ManifestEntryVerifier_Patch".equals((args[0])))
                SecureJarHandler_ManifestEntryVerifier_Patch.patchSun(libFile);
            else
                System.exit(1);
        } catch (AlreadyPatchedException ignore) {
        }
    }
}
