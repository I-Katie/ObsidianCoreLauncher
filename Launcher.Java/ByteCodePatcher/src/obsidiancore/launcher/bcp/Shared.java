package obsidiancore.launcher.bcp;

import org.apache.bcel.classfile.Method;
import org.apache.bcel.generic.ClassGen;
import org.apache.bcel.generic.InstructionList;
import org.apache.bcel.generic.MethodGen;

import java.io.IOException;
import java.nio.file.DirectoryStream;
import java.nio.file.Files;
import java.nio.file.Path;

public class Shared {
    private Shared() {
    }

    public static Method getMethod(Method[] methods, String name) {
        for (Method m : methods) {
            if (name.equals(m.getName())) {
                return m;
            }
        }
        return null;
    }

    public static Method copyMethod(ClassGen destClass, MethodGen srcMethod) {
        InstructionList il = srcMethod.getInstructionList();
        il.replaceConstantPool(srcMethod.getConstantPool(), destClass.getConstantPool());

        MethodGen methodGen = new MethodGen(
                srcMethod.getAccessFlags(),
                srcMethod.getReturnType(),
                srcMethod.getArgumentTypes(),
                srcMethod.getArgumentNames(),
                srcMethod.getName(),
                destClass.getClassName(),
                il,
                destClass.getConstantPool());
        methodGen.setMaxLocals(srcMethod.getMaxLocals());
        methodGen.setMaxStack(srcMethod.getMaxStack());

        Method newMethod = methodGen.getMethod();
        il.dispose();

        destClass.addMethod(newMethod);
        return newMethod;
    }

    public static void deleteDir(Path metaPath) throws IOException {
        if (Files.isDirectory(metaPath)) {
            try (DirectoryStream<Path> dirStr = Files.newDirectoryStream(metaPath)) {
                for (Path p : dirStr) {
                    if (Files.isDirectory(p))
                        deleteDir(p);
                    else
                        Files.delete(p);
                }
            }
            Files.delete(metaPath);
        }
    }
}
