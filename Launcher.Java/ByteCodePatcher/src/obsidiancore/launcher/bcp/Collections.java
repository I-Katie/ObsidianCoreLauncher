package obsidiancore.launcher.bcp;

import java.util.Arrays;
import java.util.Comparator;
import java.util.List;

public class Collections {
    @SuppressWarnings("unchecked")
    public static <T> void sort(List<T> list, Comparator<? super T> c) {
        T[] array = (T[]) list.toArray();
        Arrays.sort(array, c);
        for (int i = 0; i < array.length; i++) {
            list.set(i, array[i]);
        }
    }
}
