package obsidiancore.launcher.console;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

//Runs the game and displays a console that captures the game stdout and stderr.

public class Console {
    private static FileOutputStream lock;

    public static void main(String[] args) throws IOException {
        String javaBin = Decode(System.getProperty("obsidiancore.launcher.java"));
        String name = Decode(System.getProperty("obsidiancore.launcher.name"));
        String lockFile = Decode(System.getProperty("obsidiancore.launcher.lock"));
        boolean closeOnExit = "true".equals(System.getProperty("obsidiancore.launcher.closeOnExit"));

        if (lockFile.length() > 0) {
            lock = new FileOutputStream(lockFile);
        }

        String[] cmdArr = new String[args.length + 1];
        System.arraycopy(args, 0, cmdArr, 1, args.length);
        cmdArr[0] = javaBin;

        ConsoleWindow console = new ConsoleWindow(name, closeOnExit);
        try {
            Process proc = Runtime.getRuntime().exec(cmdArr, null, new File(System.getProperty("user.dir")));
            console.logProcess(proc);
        } catch (Exception ex) {
            console.appendLine(ex.getClass().getName());
            console.appendLine(ex.getMessage());
        }
    }

    private static String Decode(String input) {
        byte[] bytes = Base64.Decode(input);
        try {
            return new String(bytes, "UTF-8");
        } catch (UnsupportedEncodingException e) {
            throw new RuntimeException(e); //this is virtually impossible
        }
    }

    public static int getJavaVersionDescriptive() {
        Pattern p = Pattern.compile("^(\\d+)\\.(\\d+)");
        Matcher m = p.matcher(System.getProperty("java.version"));
        if (m.find()) {
            int major = Integer.parseInt(m.group(1));
            int minor = Integer.parseInt(m.group(2));

            if (major > 1)
                return major;
            else if (major == 1 && minor >= 1)
                return minor;
            else
                return 0;
        }
        return -1;
    }
}
