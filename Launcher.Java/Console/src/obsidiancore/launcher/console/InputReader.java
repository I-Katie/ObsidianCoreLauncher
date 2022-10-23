package obsidiancore.launcher.console;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;

public class InputReader implements Runnable {
    private final InputStream inputStream;
    private final ConsoleWindow console;

    public InputReader(InputStream inputStream, ConsoleWindow console) {
        this.inputStream = inputStream;
        this.console = console;

        Thread t = new Thread(this);
        t.setDaemon(true);
        t.start();
    }

    public void run() {
        try {
            BufferedReader br = new BufferedReader(new InputStreamReader(inputStream));
            String line;
            while ((line = br.readLine()) != null) {
                console.appendLine(line);
            }
        } catch (IOException ignore) {
        }
    }
}
