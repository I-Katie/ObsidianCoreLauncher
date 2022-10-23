package obsidiancore.launcher.bcp;

import org.apache.bcel.Repository;
import org.apache.bcel.classfile.ClassParser;
import org.apache.bcel.classfile.JavaClass;
import org.apache.bcel.classfile.Method;
import org.apache.bcel.generic.*;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.URI;
import java.nio.file.*;
import java.util.HashMap;

// Thanks to FyberOptic and their post on the minecraft forum.
// https://www.minecraftforum.net/forums/mapping-and-modding-java-edition/minecraft-mods/2206446-forge-1-6-4-1-7-2-java-8-compatibility-patch

// Patches old versions of FML so that they don't crash on Java 8.

public class CoreModManager_Sort_Patch {
    private CoreModManager_Sort_Patch() {
    }

    public static void patchSort(File libFile) throws Exception {
        //create backup
        Path copyPath = new File(libFile.getPath() + "~").toPath();
        if (!Files.exists(copyPath))
            Files.copy(libFile.toPath(), copyPath, StandardCopyOption.REPLACE_EXISTING);

        // locate file system by using the syntax defined in java.net.JarURLConnection
        URI uri = URI.create("jar:" + libFile.toURI());
        try (FileSystem zipfs = FileSystems.newFileSystem(uri, new HashMap<String, String>())) {
            //check that the jar file isn't already modified
            Path metaPath = zipfs.getPath("META-INF");
            if (!Files.isDirectory(metaPath)) throw new AlreadyPatchedException();

            //patch
            Path filePath = zipfs.getPath("cpw/mods/fml/relauncher/CoreModManager.class");
            byte[] patchedFile;
            try (InputStream is = Files.newInputStream(filePath)) {
                patchedFile = createPatchedClass(is, filePath.toString());
            }

            //overwrite patched file
            try (OutputStream os = Files.newOutputStream(filePath)) {
                os.write(patchedFile);
            }

            //delete META-INF
            Shared.deleteDir(metaPath);
        }
    }

    private static byte[] createPatchedClass(InputStream inputStream, String fileName) throws Exception {
        //load the unmodified class
        ClassParser parser = new ClassParser(inputStream, fileName);
        JavaClass classIn = parser.parse();

        ClassGen classGen = new ClassGen(classIn);
        ConstantPoolGen cp = classGen.getConstantPool();

        //add the sort method
        if (Shared.getMethod(classIn.getMethods(), "sort") != null) throw new AlreadyPatchedException();
        Method newCopy = Shared.copyMethod(classGen, getLocalSortMethod());

        //patch cpw.mods.fml.relauncher.CoreModManager.sortTweakList()
        Method origMethod = Shared.getMethod(classIn.getMethods(), "sortTweakList");
        if (origMethod == null) throw new PatchException("Couldn't find method 'sortTweakList'");

        int sortIndex = cp.addMethodref(new MethodGen(newCopy, classGen.getClassName(), cp));
        MethodGen methodGen = new MethodGen(origMethod, classIn.getClassName(), cp);

        boolean modified = false;
        InstructionList il = methodGen.getInstructionList();
        InstructionHandle[] ihs = il.getInstructionHandles();
        for (InstructionHandle ih : ihs) {
            Instruction i = ih.getInstruction();
            if (i instanceof INVOKESTATIC) {
                INVOKESTATIC is = (INVOKESTATIC) i;
                if ("java.util.Collections".equals(is.getClassName(cp))) {
                    is.setIndex(sortIndex);
                    ih.setInstruction(is);
                    modified = true;
                    break;
                }
            }
        }
        if (!modified) throw new PatchException("Couldn't patch 'sortTweakList'.");

        classGen.replaceMethod(origMethod, methodGen.getMethod());
        il.dispose();

        //write the modified class
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        classGen.getJavaClass().dump(baos);
        return baos.toByteArray();
    }

    private static MethodGen getLocalSortMethod() throws ClassNotFoundException {
        JavaClass clazz = Repository.lookupClass(Collections.class.getName());
        Method lsm = Shared.getMethod(clazz.getMethods(), "sort");
        return new MethodGen(lsm, clazz.getClassName(), new ConstantPoolGen(lsm.getConstantPool()));
    }
}
