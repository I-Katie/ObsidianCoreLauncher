package obsidiancore.launcher.console;

import java.io.ByteArrayOutputStream;

//https://base64.guru/learn/base64-algorithm/decode

public class Base64 {
    private static final String INDICES = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    public static byte[] Decode(String input) {
        ByteArrayOutputStream baos = new ByteArrayOutputStream(input.length() * 6 / 8 + 1);

        int value = 0;
        int bitCnt = 0;

        for (int i = 0; i < input.length(); i++) {
            int index = INDICES.indexOf(input.charAt(i));
            if (index > -1) {
                int bitsProvided = Math.min(6, (8 - bitCnt));
                value = value << bitsProvided;
                value |= index >>> 6 - bitsProvided;
                bitCnt += bitsProvided;

                if (bitCnt == 8) {
                    baos.write(value);
                    bitCnt = 6 - bitsProvided;
                    value = index;
                }
            }
        }

        return baos.toByteArray();
    }
}
