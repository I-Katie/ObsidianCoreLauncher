package obsidiancore.launcher.util;

//Reads the System.Properties specified via args and outputs them in a way that is simple to parse with regex.

public class GetProperties {
    public static void main(String[] args) {
        for (int i = 0; i < args.length; i++) {
            String val = System.getProperty(args[i]);
            if (val == null) val = "";
            System.out.println(args[i] + "=" + val);
        }
    }
}