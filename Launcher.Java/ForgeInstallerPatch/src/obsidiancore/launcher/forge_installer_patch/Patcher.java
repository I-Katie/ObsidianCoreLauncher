package obsidiancore.launcher.forge_installer_patch;

import javax.swing.*;
import java.awt.*;
import java.io.File;
import java.lang.reflect.Field;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.Enumeration;

//Runs the Forge installer and presets the values so that all the user has to do is click Ok twice.
//It disables everything on the main window except the OK and Cancel buttons and the affiliate link.
//This is because the Forge authors ask not to automate the installation, but I also don't want the user to
//be able to mess up. I hope this is an acceptable solution.

public class Patcher implements Runnable {
    private final File path;

    public Patcher(File path) {
        this.path = path;
    }

    public static void main(String[] args) throws Exception {
        String path = System.getProperty("obsidiancore.launcher.path");
        if (path == null) System.exit(1);

        File pathFile = new File(path).getCanonicalFile();
        if (!pathFile.isDirectory()) System.exit(1);

        Patcher main = new Patcher(pathFile);
        main.startPatcherThread();

        Class<?> cl = Class.forName("net.minecraftforge.installer.SimpleInstaller");
        Method methodMain = cl.getMethod("main", args.getClass());
        methodMain.invoke(null, new Object[]{args});
    }

    private void startPatcherThread() {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    try {
                        while (true) {
                            Thread.sleep(5);
                            EventQueue.invokeAndWait(Patcher.this);
                        }
                    } catch (InterruptedException ignore) {
                    } catch (InvocationTargetException e) {
                        throw (Exception) e.getTargetException();
                    }
                } catch (ExitException ignore) {
                } catch (WrappedException e) {
                    e.getCause().printStackTrace();
                    System.exit(2);
                } catch (Throwable e) {
                    e.printStackTrace();
                    System.exit(2);
                }
            }
        });
        thread.setDaemon(true);
        thread.start();
    }

    //event queue method
    public void run() {
        try {
            Window[] windows = Window.getWindows();
            for (int i = 0; i < windows.length; i++) {
                final Window win = windows[i];
                if (win.isShowing()) {
                    if (patch(win)) throw new ExitException();
                }
            }
        } catch (ExitException e) {
            throw e;
        } catch (Exception e) {
            throw new WrappedException(e);
        }
    }

    private boolean patch(Window win) throws Exception {
        final Container installerPanel = findInstallerPanel(win);
        if (installerPanel == null) return false;

        String choice = System.getProperty("obsidiancore.launcher.choice");
        if (choice == null) choice = "client";

        //choiceButtonGroup
        Field fieldChoiceButtonGroup = getField(installerPanel, "choiceButtonGroup");
        ButtonGroup buttonGroup = (ButtonGroup) fieldChoiceButtonGroup.get(installerPanel);
        Enumeration<AbstractButton> en = buttonGroup.getElements();
        while (en.hasMoreElements()) {
            AbstractButton ab = en.nextElement();
            String text = ab.getText();
            ab.setEnabled(false);
            if (text.toLowerCase().contains(choice)) {
                ab.setSelected(true);
            }
        }

        findButton(installerPanel, "...").setEnabled(false);

        Field fieldTargetDir = getField(installerPanel, "targetDir");
        fieldTargetDir.set(installerPanel, path);

        getMethod(installerPanel, "updateFilePath").invoke(installerPanel);

        return true;
    }

    private Container findInstallerPanel(Container container) {
        for (int i = 0; i < container.getComponentCount(); i++) {
            Component cmp = container.getComponent(i);
            if ("InstallerPanel".equals(cmp.getClass().getSimpleName())) {
                return (Container) cmp;
            } else if (cmp instanceof Container) {
                Container cont = findInstallerPanel((Container) cmp);
                if (cont != null) return cont;
            }
        }
        return null;
    }

    private AbstractButton findButton(Container container, String text) {
        for (int i = 0; i < container.getComponentCount(); i++) {
            Component cmp = container.getComponent(i);
            if (cmp instanceof JButton) {
                AbstractButton btn = (AbstractButton) cmp;
                if (text.equals(btn.getText())) return btn;
            } else if (cmp instanceof Container) {
                AbstractButton btn = findButton((Container) cmp, text);
                if (btn != null) return btn;
            }
        }
        return null;
    }

    private Field getField(Object obj, String name) {
        Field[] fields = obj.getClass().getDeclaredFields();
        for (int i = 0; i < fields.length; i++) {
            Field f = fields[i];
            if (name.equals(f.getName())) {
                if (!f.isAccessible()) f.setAccessible(true);
                return f;
            }
        }
        return null;
    }

    private Method getMethod(Object obj, String name) {
        Method[] methods = obj.getClass().getDeclaredMethods();
        for (int i = 0; i < methods.length; i++) {
            final Method m = methods[i];
            if (name.equals(m.getName())) {
                if (!m.isAccessible()) m.setAccessible(true);
                return m;
            }
        }
        return null;
    }
}
