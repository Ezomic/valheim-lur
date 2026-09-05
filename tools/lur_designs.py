"""
Lur: candidates for the horn Hildir sells.

    blender --background --python tools/lur_designs.py

The thing has one job in the fiction and one in the interface. In the fiction it is
an instrument old enough to be worth burying someone with, loud enough to carry
through stone. In the interface it is about forty-eight pixels in an inventory slot,
sitting next to a stack of coins and a key, and it has to be tellable from both at a
glance.

That second job is what actually decides the shape, because a horn is the single
easiest object in the game to render as a brown smear. Every candidate here is one
continuous tapering tube and differs from the others only in what its outline does,
since the outline is the whole of what survives the slot:

    bronze  the real Nordic lur. A long, slow S, ending in a flat ornamented disc.
    ox      a deep crescent, thick at the mouth, bound with iron.
    coil    wound once into a closed ring, hunting-horn fashion.
    stave   dead straight, a tapered bone tube with a flared bell and lashings.

An S, a crescent, a ring and a cone. Nothing here is a variation on another one, which
is the point - if two candidates share an outline there is only one design.

Two materials throughout, "bone" and "iron", and no more. Vanilla timber props are one
material on one submesh and furniture is two; three or four means wearing three or four
different objects' palettes at once, and they were never painted to sit together.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy
import math

from mathutils import Vector

from vhbuild import (box, camera, clear_scene, disc, export, finish, limb, render,
                     reference_cube, ring, stage_scene, taper)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHIPPED = os.path.join(ROOT, "assets")
ASSETS = os.path.join(SHIPPED, "variants")
PREVIEWS = os.path.join(ASSETS, "previews")

# Nothing has been picked yet. Setting this to a label exports that one a second time
# under the shipping names, so the horn you hold and the picture in the slot are the
# same geometry and cannot drift apart.
WINNER = None
SHIPPED_MESH = "lur"
SHIPPED_ICON = "lur.png"


# --------------------------------------------------------------------------- shapes

def bronze():
    """
    The real thing: a bronze-age lur, a long slow S ending in a flat disc.

    The S is the entire idea. It is the one horn outline that cannot be mistaken for a
    tusk, a bow or a piece of rope at small size, because nothing else in the inventory
    bends twice. The disc at the mouth is not decoration either - it is what stops the
    far end reading as a snapped-off tube.
    """
    # Up the first curve, thick to thin. Curve is per segment, so a small number over
    # many segments is a long slow arc rather than an elbow.
    tip = limb((0.0, 0.0, -0.30), 0.34, 5, 0.030, 0.022, 4.0, 0.0, -7.0, "bone", sides=7)

    # Back the other way. Starting from the returned tip is what keeps the join
    # invisible; setting a second limb by hand leaves a wedge of daylight on the bend.
    tip = limb(tip, 0.30, 5, 0.022, 0.017, -26.0, 0.0, 7.5, "bone", sides=7)

    # The bell, and then the plate across it. A capped cone alone is a lid and reads as
    # solid; the disc set proud of it is what says "open, and ornamented".
    taper(0.017, 0.052, 0.075, (tip.x, tip.y, tip.z + 0.03), "bone", sides=9,
          rot_x=-18.0)
    disc(0.062, 0.011, (tip.x, tip.y - 0.005, tip.z + 0.072), "iron", sides=13,
         rot_x=72.0)

    # Two bands where the curves change. They also break up the long tube, which at
    # icon size is the difference between a horn and a bent stick.
    ring(0.034, 0.008, (0.0, 0.0, -0.10), "iron", major=13, minor=5, rot_x=8.0)
    ring(0.026, 0.007, (0.055, 0.0, 0.12), "iron", major=13, minor=5, rot_x=-24.0)


def ox():
    """
    A deep crescent, thick at the mouth: the shape everyone means by "war horn".

    The most legible of the four and the least interesting, which is a real trade and
    not a criticism. It wins the slot and loses the "what is that?" - a player who has
    seen a drinking horn knows this one at a glance and learns nothing from it.

    Bound in iron at three points because a plain crescent in one material is a
    fingernail.
    """
    tip = limb((-0.14, 0.0, -0.22), 0.52, 7, 0.052, 0.014, 22.0, 0.0, 11.5, "bone",
               sides=9)

    # A slight flare rather than a bell. The mouth is the thick end on this one, so the
    # far end wants to close down to a point or the crescent stops being a crescent.
    taper(0.014, 0.019, 0.04, (tip.x, tip.y, tip.z), "bone", sides=7, rot_x=64.0)

    # The mouth ring, wide and heavy, at the other end.
    ring(0.056, 0.011, (-0.14, 0.0, -0.235), "iron", major=15, minor=6, rot_x=22.0)
    ring(0.042, 0.008, (-0.055, 0.0, -0.055), "iron", major=13, minor=5, rot_x=8.0)
    ring(0.028, 0.007, (0.055, 0.0, 0.10), "iron", major=13, minor=5, rot_x=-14.0)


def coil():
    """
    Wound once into a closed ring.

    The only candidate with a hole in it, and a hole is worth a great deal in a grid of
    small brown objects - it is the one silhouette feature that survives being drawn at
    any size at all, because the background shows through it.

    The risk is the opposite one: at slot size a ring reads as a bracelet or a coil of
    rope unless the bell is unmistakably a bell. So the bell here is deliberately
    oversized against the loop.
    """
    # The loop itself. Major segments high enough that it reads as round rather than as
    # a polygon, minor low - it is a tube, and nobody counts its facets.
    ring(0.155, 0.026, (0.0, 0.0, 0.0), "bone", major=21, minor=7, rot_x=6.0)

    # The bell, growing off the loop and away from it. Angled out of the ring's plane so
    # the silhouette is not perfectly flat, which is what would make it read as a
    # printed circle rather than an object.
    taper(0.030, 0.088, 0.135, (0.135, 0.02, 0.105), "bone", sides=9, rot_x=-52.0,
          rot_y=16.0)

    # Mouthpiece, small and on the far side, so the eye can find which end is which.
    taper(0.026, 0.019, 0.055, (-0.150, -0.01, -0.075), "bone", sides=7, rot_x=34.0)

    # One band at the throat and one where the loop closes.
    ring(0.036, 0.009, (0.118, 0.012, 0.075), "iron", major=13, minor=5, rot_x=-52.0)
    ring(0.031, 0.008, (-0.146, -0.006, -0.052), "iron", major=13, minor=5, rot_x=34.0)


def stave():
    """
    Dead straight: a bone tube, a flared bell, and lashings.

    The crude one, and the only one that looks made rather than found. A straight cone
    is the weakest outline of the four and it buys back the difference in texture - the
    lashings give it a rhythm along its length that the others get from bending.

    Boxes for the lashings rather than rings, because a wrapped cord is flat-sided where
    it crosses the tube and a torus reads as another metal band, which this already has
    two of.
    """
    taper(0.021, 0.040, 0.62, (0.0, 0.0, 0.0), "bone", sides=9)

    # The bell. Wide and shallow: a long flare turns the whole thing into a funnel and
    # the tube stops being the subject.
    taper(0.040, 0.082, 0.10, (0.0, 0.0, 0.345), "bone", sides=11)

    # Mouthpiece.
    taper(0.024, 0.017, 0.05, (0.0, 0.0, -0.325), "bone", sides=7)

    # Three lashings, and a fourth would be a barber's pole. Each is four short boxes
    # around the tube rather than a ring, and they are set at slightly different heights
    # on each face so the wrap reads as wound rather than as a machined collar.
    for at, radius in ((-0.16, 0.026), (0.02, 0.031), (0.20, 0.036)):
        for i in range(4):
            angle = math.radians(90.0 * i + 12.0)
            box((0.020, 0.010, 0.030),
                (math.cos(angle) * radius, math.sin(angle) * radius, at + i * 0.004),
                "iron", rot_z=math.degrees(angle))

    ring(0.044, 0.010, (0.0, 0.0, 0.335), "iron", major=15, minor=5, rot_x=0.0)


DESIGNS = [
    ("bronze", bronze),
    ("ox", ox),
    ("coil", coil),
    ("stave", stave),
]


# --------------------------------------------------------------------------- staging

def preview_scene(obj):
    """
    Close, three quarters on, with a 25cm block for scale.

    Not the eye-height-and-a-1m-cube staging the buildable pieces use: nobody ever sees
    a horn from three metres away standing on the ground. It is seen in a slot and in a
    hand, so it is staged at the distance a hand would hold it, and the reference is a
    block the size of a fist rather than a cube the size of a doorway.
    """
    stage_scene(sun=1.4)

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = [min(c[i] for c in corners) for i in range(3)]
    hi = [max(c[i] for c in corners) for i in range(3)]
    centre = [(lo[i] + hi[i]) * 0.5 for i in range(3)]

    # Sat on the ground rather than floating, so the eye has somewhere to put it.
    obj.location.z -= lo[2]
    centre[2] -= lo[2]

    reference_cube((0.42, 0.0, 0.125))
    bpy.context.active_object.scale = (0.25, 0.25, 0.25)

    camera((0.62, -0.95, 0.52), (centre[0], centre[1], centre[2]), lens=50)


def icon_scene(obj):
    """
    Orthographic, three quarters on, transparent, fitted.

    Three quarters rather than front on, which the sapling icon already paid for: dead
    front on flattens a small object into a symmetrical blob, and the whole job of an
    icon is an outline you can tell from its neighbours in a grid.

    Suns, not area lights. Area lights a metre from a 20cm object blow every channel to
    white, and the tell is dark brown rendering as pale beige.
    """
    scene = bpy.context.scene
    scene.render.film_transparent = True

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = [min(c[i] for c in corners) for i in range(3)]
    hi = [max(c[i] for c in corners) for i in range(3)]

    centre = [(lo[i] + hi[i]) * 0.5 for i in range(3)]
    span = max(hi[i] - lo[i] for i in range(3))

    target = bpy.data.objects.new("aim", None)
    bpy.context.collection.objects.link(target)
    target.location = centre

    azimuth, elevation, distance = math.radians(34.0), math.radians(17.0), 1.6

    bpy.ops.object.camera_add(location=(
        centre[0] + math.sin(azimuth) * math.cos(elevation) * distance,
        centre[1] - math.cos(azimuth) * math.cos(elevation) * distance,
        centre[2] + math.sin(elevation) * distance))

    cam = bpy.context.active_object
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = span * 1.14

    track = cam.constraints.new(type="TRACK_TO")
    track.target = target
    track.track_axis = "TRACK_NEGATIVE_Z"
    track.up_axis = "UP_Y"
    scene.camera = cam

    bpy.ops.object.light_add(type="SUN", location=(-0.6, -1.0, 0.7))
    key = bpy.context.active_object
    key.data.energy = 2.8
    key.rotation_euler = (math.radians(56.0), 0.0, math.radians(-34.0))

    bpy.ops.object.light_add(type="SUN", location=(0.8, -0.9, -0.3))
    fill = bpy.context.active_object
    fill.data.energy = 1.0
    fill.rotation_euler = (math.radians(104.0), 0.0, math.radians(36.0))

    world = bpy.data.worlds.new("icon_world")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.0


def build(label, maker):
    name = "lur_" + label
    winner = label == WINNER

    clear_scene()
    maker()
    obj = finish(name)
    export(obj, name, ASSETS)
    tris = len(obj.data.polygons)

    if winner:
        export(obj, SHIPPED_MESH, SHIPPED)

    # A fresh scene per render rather than reusing the one just exported from. The icon
    # pass sets film_transparent, which is scene state - left on, the next preview comes
    # out with a white void for a sky, which reads as a blown exposure and is hunted in
    # the lighting.
    clear_scene()
    maker()
    obj = finish(name)
    preview_scene(obj)
    render(os.path.join(PREVIEWS, name + "_hand.png"), width=620, height=580,
           bloom=False)

    clear_scene()
    maker()
    obj = finish(name)
    icon_scene(obj)

    # 128, not 64. Valheim scales icons down and a sharp source survives that better
    # than one rendered at the size it will be shown at.
    render(os.path.join(PREVIEWS, name + "_icon.png"), width=128, height=128,
           bloom=False)
    render(os.path.join(PREVIEWS, name + "_large.png"), width=512, height=512,
           bloom=False)

    if winner:
        render(os.path.join(SHIPPED, SHIPPED_ICON), width=128, height=128, bloom=False)

    print("DESIGN_OK %s tris=%d%s" % (name, tris, " [SHIPPED]" if winner else ""))


def main():
    os.makedirs(PREVIEWS, exist_ok=True)

    for label, maker in DESIGNS:
        build(label, maker)


main()
