# Clientul de producție folosește kotlinx.serialization + Retrofit; reguli necesare la minificare.
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.**

-keepclassmembers class kotlinx.serialization.json.** {
    *** Companion;
}
-keepclasseswithmembers class com.omnichannel.store.data.remote.dto.** {
    kotlinx.serialization.KSerializer serializer(...);
}
