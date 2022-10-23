package obsidiancore.launcher.bcp;

import org.apache.bcel.classfile.*;
import org.apache.bcel.generic.*;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.InputStream;
import java.io.OutputStream;
import java.lang.reflect.Field;
import java.net.URI;
import java.nio.file.*;
import java.util.HashMap;

// So, someone thought it was a good idea to use sun.security.util.ManifestEntryVerifier
// which was removed from a newer version of Java 8 (as the documentation around the sun package clearly states can happen).
// This code nulls a static field in the class that does the "unspeakable thing" to prevent it from crashing.

public class SecureJarHandler_ManifestEntryVerifier_Patch {
    private static final String PATCH_MARK = "obsidiancore.launcher.bcp.sunpatch";

    private SecureJarHandler_ManifestEntryVerifier_Patch() {
    }

    public static void patchSun(File libFile) throws Exception {
        //create backup
        Path copyPath = new File(libFile.getPath() + "~").toPath();
        if (!Files.exists(copyPath))
            Files.copy(libFile.toPath(), copyPath, StandardCopyOption.REPLACE_EXISTING);

        URI uri = URI.create("jar:" + libFile.toURI());
        try (FileSystem zipfs = FileSystems.newFileSystem(uri, new HashMap<String, String>())) {
            //patch
            Path filePath = zipfs.getPath("cpw/mods/modlauncher/SecureJarHandler.class");
            byte[] patchedFile;
            try (InputStream is = Files.newInputStream(filePath)) {
                patchedFile = createPatchedClass(is, filePath.toString());
            }

            //overwrite patched file
            try (OutputStream os = Files.newOutputStream(filePath)) {
                os.write(patchedFile);
            }
        }
    }

    private static byte[] createPatchedClass(InputStream inputStream, String fileName) throws Exception {
        //load the unmodified class
        ClassParser parser = new ClassParser(inputStream, fileName);
        JavaClass classIn = parser.parse();

        ClassGen classGen = new ClassGen(classIn);
        ConstantPoolGen cp = classGen.getConstantPool();

        //check if already patched
        for (int i = 0; i < cp.getSize(); i++) {
            Constant c = cp.getConstant(i);
            if (c instanceof ConstantString) {
                ConstantString cs = (ConstantString) c;
                String val = (String) cs.getConstantValue(cp.getConstantPool());
                if (PATCH_MARK.equals(val)) throw new AlreadyPatchedException();
            }
        }

        Method origMethod = Shared.getMethod(classIn.getMethods(), "<clinit>");
        MethodGen methodGen = new MethodGen(origMethod, classIn.getClassName(), cp);

        InstructionList il = methodGen.getInstructionList();
        il.delete(il.getInstructions()[il.size() - 1]);

        InstructionFactory f = new InstructionFactory(cp);
        il.append(new ACONST_NULL());
        il.append(f.createPutStatic(classIn.getClassName(), "JV", Type.getType(Field.class)));
        il.append(new RETURN());

        Method newMethod = methodGen.getMethod();

        classGen.removeMethod(origMethod);
        classGen.addMethod(newMethod);

        cp.addString(PATCH_MARK);

        //write the modified class
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        classGen.getJavaClass().dump(baos);
        return baos.toByteArray();
    }
}
