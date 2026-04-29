from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class AppProfile:
    label: str
    package: str
    display_name: str
    icon: str


KIDS_APPS: tuple[AppProfile, ...] = (
    AppProfile("Kids 3D", "com.sec.android.app.kids3d", "Crocro's Friend Village", "village.png"),
    AppProfile("Music 3D", "com.sec.kidsplat.media.kidsmusic", "Lisa's Music Band", "musicband.png"),
    AppProfile("Magic Voice", "com.sec.kidsplat.kidstalk", "My Magic Voice", "voice.png"),
    AppProfile("Adventure", "com.sec.kidsplat.kidsbcg", "Crocro's Adventure", "adventure.png"),
    AppProfile("Browser", "com.sec.kidsplat.kidsbrowser", "My Browser", "browser.png"),
    AppProfile("Phone", "com.sec.kidsplat.phone", "My Phone", "call.png"),
    AppProfile("Camera", "com.sec.kidsplat.camera", "My Camera", "camera.png"),
    AppProfile("My Gallery", "com.sec.kidsplat.kidsgallery", "My Gallery", "gallery.png"),
    AppProfile("Cooki's", "br.org.sidi.kidsplat.collection", "Cooki's Collection", "lego.png"),
    AppProfile("Art Studio", "br.org.sidi.kidsplat.artstudio", "My Art Studio", "studio.png"),
    AppProfile("Trav. Buddies", "br.org.sidi.kidsplat.travel", "Travel Buddies", "travel.png"),
    AppProfile("Kids Home", "com.sec.android.app.kidshome", "Kids Home", "home.png"),
)

PARENTAL_PACKAGE = "com.samsung.android.app.parentalcare"
CARE_SAMPLE_PACKAGE = "com.samsung.android.app.caresample"
