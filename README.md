# BetterSerialMonitor
The serial monitor included in Arduino is limited, especially in the fact that you can really only send human-readable ASCII text. Sometimes you need to be able to easily test things, and sometimes those tests require sending bytes over the serial line that are not human-readable ASCII characters.

This one is a little bit better. It allows you to construct a message out of text or just raw bytes. 

When adding bytes to your message, you can choose to enter a decimal number or a hexidecimal number. Separate each byte by a space. Hexidecimal numbers should be prefixed by "0x".

Have fun!

- David
