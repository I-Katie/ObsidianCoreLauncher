package obsidiancore.launcher.console;

import java.awt.*;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.StringSelection;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.WindowEvent;
import java.awt.event.WindowListener;

public class ConsoleWindow extends Frame implements ActionListener, WindowListener {

    private final TextArea textArea;
    private final Button buttonClear, buttonCopyAll, buttonKill;

    private final Clipboard clip;
    private final Checkbox checkboxCloseOnExit;

    private boolean exited = false;
    private Process proc;

    public ConsoleWindow(String name, boolean closeOnExit) {
        super(name + " - Minecraft Console");

        clip = getToolkit().getSystemClipboard();

        addWindowListener(this);

        setLayout(new BorderLayout());

        textArea = new TextArea("", 30, 80, TextArea.SCROLLBARS_VERTICAL_ONLY);
        textArea.setEditable(false);
        add(textArea, BorderLayout.CENTER);

        Panel bottomPanel = new Panel();
        bottomPanel.setLayout(new FlowLayout(FlowLayout.LEFT));

        buttonClear = new Button("Clear");
        buttonClear.addActionListener(this);
        bottomPanel.add(buttonClear);

        buttonCopyAll = new Button("Copy all");
        buttonCopyAll.addActionListener(this);
        bottomPanel.add(buttonCopyAll);

        buttonKill = new Button("Kill Minecraft");
        buttonKill.addActionListener(this);
        buttonKill.setEnabled(false);
        bottomPanel.add(buttonKill);

        checkboxCloseOnExit = new Checkbox("Close on game exit", closeOnExit);
        checkboxCloseOnExit.setEnabled(false);
        bottomPanel.add(checkboxCloseOnExit);

        add(bottomPanel, BorderLayout.SOUTH);

        pack();
        setVisible(true);
    }

    public void actionPerformed(ActionEvent e) {
        if (e.getSource() == buttonClear) {
            textArea.setText("");
        } else if (e.getSource() == buttonCopyAll) {
            clip.setContents(new StringSelection(textArea.getText()), null);
        } else if (e.getSource() == buttonKill) {
            try {
                proc.destroy();
            } catch (Throwable ignore) {
            }
        }
    }

    public void logProcess(final Process proc) {
        this.proc = proc;
        buttonKill.setEnabled(true);
        checkboxCloseOnExit.setEnabled(true);

        new InputReader(proc.getInputStream(), this);
        new InputReader(proc.getErrorStream(), this);

        Thread exitThread = new Thread(new Runnable() {
            public void run() {
                try {
                    final int exitCode = proc.waitFor();
                    Thread.sleep(200); //wait for the buffers to empty
                    EventQueue.invokeLater(new Runnable() {
                        public void run() {
                            processExited(exitCode);
                        }
                    });
                } catch (InterruptedException ignore) {
                }
            }
        });
        exitThread.setDaemon(true);
        exitThread.start();
    }

    void appendLine(final String text) {
        EventQueue.invokeLater(new Runnable() {
            public void run() {
                textArea.append(text);
                textArea.append("\n");
            }
        });
    }

    private void processExited(int exitCode) {
        exited = true;
        buttonKill.setEnabled(false);
        checkboxCloseOnExit.setEnabled(false);

        if (exitCode == 0) {
            if (checkboxCloseOnExit.getState()) {
                System.exit(0);
            }
        }

        appendLine("Exited with code: " + exitCode);
    }

    public void windowOpened(WindowEvent e) {
        // TODO: this one (is this ok on Linux and Mac?)
        if (Console.getJavaVersionDescriptive() <= 8) { //maybe others too?
            //make the font and window larger
            textArea.setFont(textArea.getFont().deriveFont(textArea.getFont().getSize() + 4f));
            pack();
        }
    }

    public void windowClosing(WindowEvent e) {
        if (exited || (proc == null)) {
            System.exit(0);
        }
    }

    public void windowClosed(WindowEvent e) {
    }

    public void windowIconified(WindowEvent e) {
    }

    public void windowDeiconified(WindowEvent e) {
    }

    public void windowActivated(WindowEvent e) {
    }

    public void windowDeactivated(WindowEvent e) {
    }
}
