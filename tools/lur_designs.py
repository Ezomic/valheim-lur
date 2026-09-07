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
    wolf    a real blowing horn, from a photograph Robbin sent. Shipped until scroll.

    scroll  the one that ships. A ram's horn, wound three quarters of a turn, tightening
            as it goes.
    crook   a shaft that turns hard into its bell: the horn sounded in a fight.

An S, a crescent, a ring, a cone, and the real thing. Nothing here is a variation on
another one, which is the point - if two candidates share an outline there is only one
design. The first four were built before the photograph and are kept because the
reasoning in them is why wolf is shaped as it is: ox in particular is the same object
built backwards, and the difference between the two is the whole lesson.

The last two came later and answer a different question. wolf was settled and shipped when
they were drawn, so they are not attempts to beat it on its own terms - a second gentle arc
would only be wolf drawn worse. They take two outlines the first five left unclaimed: mass instead of
a line, and one hard turn instead of an even one. Both are built on run() rather than on
a single sweep, because both change their curvature along their length and a sweep is a
circle.

A third, twin, was a mirrored pair on a shared mouthpiece block, which is how bronze-age
lurs are actually found. It is gone. Symmetry does survive being drawn small, and that
was the whole argument for it, but two tubes rising off a block is a horned helmet with
a bar across it however carefully the bar is placed. Robbin's verdict was one word, and
it was the right one. run() and the rest of the machinery it needed stay; the mirroring
helper went with it, since nothing else here has two of anything.

Two materials throughout, "bone" and "iron", and no more. Vanilla timber props are one
material on one submesh and furniture is two; three or four means wearing three or four
different objects' palettes at once, and they were never painted to sit together.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy
import math

from mathutils import Euler, Vector

from vhbuild import (box, camera, clear_scene, disc, export, finish, limb, material,
                     render,
                     reference_cube, ring, stage_scene, taper)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHIPPED = os.path.join(ROOT, "assets")
ASSETS = os.path.join(SHIPPED, "variants")
PREVIEWS = os.path.join(ASSETS, "previews")

# The winner is exported a second time under the shipping names, so the horn you hold
# and the picture in the slot are the same geometry and cannot drift apart. Everything
# else stays in variants\, which the build does not copy.
#
# scroll as of 2026-09-07, and wolf before it. wolf was the photograph and it was a good
# horn; it lost to the one thing an outline cannot buy back, which is that a coiled horn is
# not a shape anything else in the inventory has. Nothing about wolf is deprecated - it is
# still the reference for how a blowing horn is proportioned, and scroll's bell is its bell.
WINNER = "scroll"
SHIPPED_MESH = "lur"
SHIPPED_ICON = "lur.png"

# How far each candidate is turned for its portrait, where it is not the usual half turn.
# Only the ones built standing the other way up need an entry, and wolf must never get one:
# the shipped icon is rendered through this path, and the last time its angle was re-derived
# from parameters it came back subtly different from the picture that had been approved.
ICON_TURN = {}


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


def sweep(name, mat, length, base_r, tip_r, bend, rings=26, sides=12, taper_power=1.35,
          origin=(0.0, 0.0, 0.0), lean=0.0, arc_start=0.0, curve_radius=None,
          cap=True):
    """
    A tapering tube swept along a smooth arc, built as one mesh.

    This exists because vhbuild's limb() is the wrong tool for a horn and the first
    attempt proved it in one render. limb() chains separate cones for gnarled branches,
    and a chain of cones with a bevel pass on every rim reads as a screw thread - which
    is exactly what it looked like. Nothing about that is fixable by tuning it: the rings
    are the segments, and the segments are the point of limb().

    So the body is one mesh with continuous topology. Rings of vertices are placed along
    a circular arc, each perpendicular to the local tangent, radii interpolated along the
    length, and consecutive rings bridged with quads. No joins, no rims, no bevel.

    taper_power bends the radius curve. A straight lerp gives a cone, and a cone is not a
    horn - real horn keeps its width high up and then falls away quickly near the tip,
    which is what a power above 1 does.

    Returns the tip position, so the mouthpiece can be hung off it without guessing.
    """
    import bmesh

    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    bm = bmesh.new()

    # A UV layer, made up front, because a swept mesh has none by construction and the first
    # version shipped without one. Every face then exported as f 1/1/1 - one shared UV index for
    # the whole horn - so the borrowed vanilla material sampled a single texel and the model came
    # out a flat wash of one colour. It looked right in Blender, where the preview material does
    # not care, and wrong in game, where every surface is a strip of an atlas.
    uvs = bm.loops.layers.uv.new()

    # The arc, in the XZ plane. bend is total turn in degrees over the whole length; the
    # radius of curvature falls out of it, so length and bend are independent knobs.
    #
    # arc_start and curve_radius exist so a second sweep can *continue* the first one's
    # curve exactly - which is how the mouthpiece is fitted. Rotating a cone onto the end
    # by hand was the first attempt and it produced a peg sticking out sideways, because
    # the arc lies in XZ and a tilt toward it is a rotation about Y, not the X that
    # vhbuild's helpers offer. Continuing the sweep sidesteps the whole question: the
    # fitting cannot be misaligned because it is the same curve.
    total = math.radians(max(1.0, bend))
    if curve_radius is None:
        curve_radius = length / total
    start = math.radians(arc_start)
    lean_rad = math.radians(lean)

    previous = None
    previous_v = 0.0
    for i in range(rings):
        t = i / float(rings - 1)
        angle = start + total * t

        # Centre of this ring, and the tangent it has to sit square to. Measured from the
        # arc's own origin, so a continuation lands exactly where its parent ended.
        cx = curve_radius * (1.0 - math.cos(angle))
        cz = curve_radius * math.sin(angle)
        centre = Vector((cx, 0.0, cz))

        tangent = Vector((math.sin(angle), 0.0, math.cos(angle)))
        tangent.rotate(Euler((0.0, lean_rad, 0.0), "XYZ"))
        centre.rotate(Euler((0.0, lean_rad, 0.0), "XYZ"))
        centre += Vector(origin)

        radius = tip_r + (base_r - tip_r) * ((1.0 - t) ** taper_power)

        # Any vector not parallel to the tangent works as an up reference; the horn's arc
        # never approaches vertical in y, so y is always safe here.
        side = tangent.cross(Vector((0.0, 1.0, 0.0))).normalized()
        up = side.cross(tangent).normalized()

        ring_verts = []
        for s in range(sides):
            a = 2.0 * math.pi * s / float(sides)
            offset = side * (math.cos(a) * radius) + up * (math.sin(a) * radius)
            ring_verts.append(bm.verts.new(centre + offset))

        if previous is not None:
            for s in range(sides):
                n = (s + 1) % sides

                face = bm.faces.new((previous[s], previous[n], ring_verts[n], ring_verts[s]))

                # Cylinder projection: u around the ring, v along the sweep. Taken per face
                # rather than per vertex so the seam can run from 1.0 back to 0.0 without the
                # last column of quads being stretched the whole way round the horn.
                u0 = s / float(sides)
                u1 = (s + 1) / float(sides)

                face.loops[0][uvs].uv = (u0, previous_v)
                face.loops[1][uvs].uv = (u1, previous_v)
                face.loops[2][uvs].uv = (u1, t)
                face.loops[3][uvs].uv = (u0, t)

        previous = ring_verts
        previous_v = t

    # Cap the narrow end only. The bell is open, which is the whole difference between a
    # horn and a cone - a capped cone is a lid, and it reads as one.
    if cap:
        cap_face = bm.faces.new(previous)

        # The cap is a disc, so it gets a disc's own unwrap rather than a slice of the tube's.
        # Small and end-on, so precision here buys nothing - what it buys is not sampling the
        # single texel the whole mesh used to share.
        for index, loop in enumerate(cap_face.loops):
            a = 2.0 * math.pi * index / float(len(cap_face.loops))
            loop[uvs].uv = (0.5 + 0.5 * math.cos(a), 0.5 + 0.5 * math.sin(a))

    # Read the tip out before the bmesh is freed. Touching a BMVert afterwards raises
    # "BMesh data of type BMVert has been removed", which is a real error rather than a
    # stale value, so it fails loudly - but only at the moment the caller uses the tip.
    tip = Vector((0.0, 0.0, 0.0))
    for v in previous:
        tip += Vector(v.co)
    tip /= float(len(previous))

    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()

    mesh.materials.append(material(mat))

    return tip


def run(name, mat, start, heading, length, bend, base_r, tip_r, rings=None, sides=13,
        taper_power=1.0, cap=False):
    """
    One length of horn with a curvature of its own, laid exactly on the end of the last.

    sweep() draws a circular arc, so a horn whose curvature changes along its length -
    a scroll that tightens as it winds, a shaft that runs straight and then turns hard
    into its bell - cannot be one sweep. It has to be several, and the joins are the
    whole difficulty. A second sweep positioned by eye is the peg sticking out sideways
    that the mouthpiece already cost once, and no amount of nudging the numbers fixes
    it, because the error is in what is being lined up rather than in how well.

    sweep's own arc_start and lean solve it outright. At arc_start 0 the arc passes
    exactly through its origin and its tangent there is `lean` degrees off +Z, so a run
    that starts at the previous run's end point, leaning along the heading the previous
    run left on, continues that curve exactly - however different its radius of
    curvature. Nothing is measured off a render and nothing can drift.

    Headings are degrees from +Z toward +X, the convention the arc already uses inside
    sweep, and bend is always a turn in that direction. A horn that should curl the
    other way is aimed the other way round rather than given a negative bend, which
    sweep clamps away.

    Returns the point and heading it ended on, ready for the next run.
    """
    turn = math.radians(max(1.0, bend))
    radius = length / turn

    # Rings from the turn rather than per call. Twelve degrees a ring is below where the
    # facets read on a tube this thin, and a run that is nearly straight has no use for
    # thirty rings to say so.
    if rings is None:
        rings = max(4, int(bend / 12.0) + 3)

    sweep(name, mat, length=length, base_r=base_r, tip_r=tip_r, bend=bend, rings=rings,
          sides=sides, taper_power=taper_power, origin=tuple(start), lean=heading,
          arc_start=0.0, curve_radius=radius, cap=cap)

    # Where the arc came out, in the same frame sweep put it. Read from the arc rather
    # than from sweep's returned tip, which is the centre of the last ring and therefore
    # already a millimetre or two off the curve on a tight bend.
    end = Vector((radius * (1.0 - math.cos(turn)), 0.0, radius * math.sin(turn)))
    end.rotate(Euler((0.0, math.radians(heading), 0.0), "XYZ"))

    return Vector(start) + end, heading + bend


def band(name, at, heading, radius, length=0.022, bend=5.0, mat="iron"):
    """
    A short iron sleeve sitting proud on the curve, dropped at a join.

    Rings were the first attempt and a torus is the wrong object here: its own axis has
    to be aimed along the tube, and the tube's direction lies in XZ, which is a rotation
    about Y that vhbuild's ring() does not offer. Every band came out as a collar worn
    at an angle. A short run of the same curve cannot be aimed wrongly, because it is
    not being aimed at all.
    """
    run(name, mat, at, heading, length, bend, radius, radius, sides=13)


def wolf():
    """
    The one Robbin picked out: a real blowing horn, from a photograph.

    Everything about it is the opposite way round from `ox`, and that is the whole
    correction. `ox` is thick where you hold it and tapers to a point, which is a
    drinking horn drawn from memory. A horn you *blow* is wide at the open end and
    narrows to the mouthpiece, so the mass sits high and the dark tip is the small end.
    Getting that backwards is why `ox` reads as a tusk.

    Three things carry it, in the order they matter at slot size:

    The taper. Wide open bell, long gentle single curve, narrow tip - one arc, not a
    crescent. A deep bend would put the tip back under the bell and close the outline
    into a claw.

    The dark mouthpiece. Real horn is near-black at the tip and pale at the bell, and
    that split is worth more than any amount of surface detail: it gives the silhouette
    a light end and a dark end, so even at 48 pixels the eye knows which way round it is.

    The medallion. A shallow raised disc on the flank, where the photograph has its
    carved wolf. The carving itself is far below anything that survives being drawn
    small, and it is not attempted - but the disc catches light along its rim and says
    "somebody's horn" rather than "a horn", which is the part that does survive.

    Still two materials. The pale body is "bone"; the dark tip, the collar and the
    medallion are all "iron", which is doing double duty as dark polished horn. Vanilla
    furniture is two submeshes and timber props are one; three would be wearing a third
    object's palette for the sake of a detail nobody can resolve.
    """
    # One arc, shared by every part of the horn. Body, collar and mouthpiece are three
    # sweeps along it at different angles, so the fittings cannot drift or sit crooked -
    # they are literally the same curve, continued.
    bend = 78.0
    radius = 0.66 / math.radians(bend)

    # The body: bell at the origin, tapering away. 78 degrees rather than the 37 the
    # first attempt had - the photograph's horn turns much more than it looks like it
    # does, and an under-bent horn reads as a tusk.
    #
    # Open at the bell, so no cap. A capped cone is a lid and reads as one, and the open
    # mouth is the whole difference between a horn and a spike.
    sweep("horn_body", "bone", length=0.66, base_r=0.079, tip_r=0.016, bend=bend,
          rings=30, sides=13, taper_power=1.45, curve_radius=radius, cap=False)

    # No separate bell lip. The first version had one and it read as a stepped cuff
    # slipped over the end, because two sweeps meeting at a shared radius still show
    # their seam under flat shading. The body's own taper is the bell.

    # The collar, then the mouthpiece: short continuations of the same arc, starting
    # where the body ends. The collar is slightly proud of the body, which is what makes
    # the dark tip read as pushed on rather than as a shadow.
    sweep("horn_collar", "iron", length=0.030, base_r=0.023, tip_r=0.021, bend=3.6,
          rings=4, sides=13, taper_power=1.0, arc_start=bend - 2.0, curve_radius=radius,
          cap=False)

    sweep("horn_mouth", "iron", length=0.075, base_r=0.019, tip_r=0.015, bend=9.0,
          rings=8, sides=11, taper_power=1.0, arc_start=bend + 1.0, curve_radius=radius)

    # The medallion, on the flank below the bell, on the outside of the curve where the
    # light is. A shallow plate and a slightly wider backing ring for the beaded border -
    # at icon size the border is the only part of the carving that reads at all, and the
    # carving itself is far below anything that survives being drawn small.
    # A dark rim just proud of a pale roundel. The backing was 10mm wider than the face and
    # emerged from the curved flank on one side only, which read as a shadow cast by a button
    # rather than as a border - the tell being that it was a crescent instead of a ring. 4mm of
    # difference is enough to draw a rim and not enough to escape.
    # Sat far enough out to clear the flank entirely.
    #
    # The first two attempts read as a pale button with a dark crescent behind it, and the
    # radius was never the problem. The horn tapers, so its flank is a cone rather than a
    # cylinder: a disc held flat against -Y buries the edge toward the thick end and leaves the
    # edge toward the thin end standing proud, which draws exactly that crescent. Moving it out
    # until the whole disc clears is the fix; tilting it to the local surface angle would be the
    # other one and costs a frame calculation for a detail that is four pixels in the slot.
    disc(0.036, 0.004, (0.016, -0.070, 0.130), "iron", sides=17, rot_x=90.0)
    disc(0.032, 0.008, (0.016, -0.073, 0.130), "bone", sides=17, rot_x=90.0)


def scroll():
    """
    A ram's horn, wound three quarters of a turn and tightening as it goes.

    The only candidate in the set that is mass rather than a line, and that is the whole
    reason for it. Every other horn here is a stroke drawn across the slot, and a stroke
    is what a horn has in common with a stick, a bow, a branch and a length of rope - all
    of which a player also owns. A curl has that in common with nothing.

    It is not coil. coil is one closed ring of even thickness with daylight through the
    middle, a hunting horn folded up for carrying, and its whole trade is that hole. This
    never closes and never repeats a radius: it starts wide and slack at the bell and
    finishes tight and thin at the tip, so the outline walks the eye round on its own.
    Tightening is what stands in for the hole, and the two are not interchangeable - put
    a hole in this and it becomes coil with a bulge.

    Three quarters, and not the turn and a bit the first version wound. That one finished
    with the mouthpiece in the middle of its own whorl, which is a horn nobody can blow:
    the coil reaches your cheekbone a long way before the tip reaches your lips. It had
    been checked as an outline and never once as a hold, and the object it was drawn from
    is the answer to it - a real ram's horn winds less than a full turn precisely so the
    narrow end stays clear of the rest of it.

    So the winding stops at 250 degrees, while the tip is still heading away from the
    coil rather than back into it, and it ends with a dark mouthpiece standing free at
    the mouth of the C. That costs the dark centre the wound version had, which was the
    better landmark of the two. A horn that cannot be sounded is not a horn, so it is not
    a trade worth arguing over.

    Built from the bell round rather than from the tip out, which is the same shape either
    way and not the same code: sweep caps its last ring, so going this way round puts the
    cap on the tip, where a horn is closed, for nothing.
    """
    at, head = Vector((0.0, 0.0, 0.0)), 0.0

    # The bell, and the first quarter turn out of it. Slack: a bell that starts turning
    # hard is a funnel bent in a vice.
    at, head = run("scroll_bell", "bone", at, head, 0.24, 72.0, 0.086, 0.058, cap=False)
    band("scroll_lip", at, head, 0.062)

    # Then tighter, and thinner with it. The two have to move together - a tube that keeps
    # its width while the curve closes reads as a hose, and one that thins while the curve
    # stays open reads as a whip.
    at, head = run("scroll_wide", "bone", at, head, 0.19, 88.0, 0.058, 0.042)
    at, head = run("scroll_mid", "bone", at, head, 0.13, 65.0, 0.042, 0.030)
    band("scroll_throat", at, head, 0.034)

    # The mouthpiece, out in the open where a mouth can reach it. Twenty five degrees and
    # no more: the tip has to still be travelling away from the coil when it stops, and a
    # few degrees past that it is pointing back into the gap it just left.
    run("scroll_mouth", "iron", at, head, 0.075, 25.0, 0.028, 0.016, sides=11, cap=True)


def crook():
    """
    The one you sound in a fight: a shaft that turns hard into its bell.

    Straight-then-suddenly-not is the last outline the other candidates leave alone.
    bronze bends twice and slowly, wolf bends once and evenly, stave never bends at all,
    and the eye reads all three as one continuous gesture. This is two gestures with a
    corner between them, and the corner is what lets it be big without being long.

    The corner is also where a horn looks broken. A curl set on the end of a straight
    tube by hand shows the join as a kink, and a kink in a horn is a crack. run() is what
    makes it survive: shaft and curl are one curve with two radii, so there is no join to
    see, only a place where the same tube starts turning much harder.

    Two rounds got it here and the second one is the useful one. The first was a tobacco
    pipe - an even taper into a deep curl is a bowl on a stem, whatever its diameter, and
    depth is what the eye reads as "holds something". Fixing that made a bugle: a shallow
    wide flare with a polished iron band on the lip, a long thin straight run of pipe, and
    every part of it perfectly round about a perfectly smooth curve. Nothing there is
    wrong for a trumpet. It is wrong for this, because a trumpet is owned by an orchestra
    and this is carried by somebody who is about to be in a fight.

    Four things separate the two, and none of them is the outline:

    The metal is off the mouth. A brass bell is thinnest at the lip and a real horn is
    thickest there, so the rim is bone now and heavy, which is most of the difference at
    a glance.

    The bell is a cone. Anything that opens faster than a cone is a curve somebody
    calculated, and it is the single loudest thing a shape can say about how it was made.

    The straight run is shorter and fatter. At 36cm of thin pipe the object read as
    tubing with a horn on the end; at 30cm and thicker it reads as a horn with a handle,
    which is what a signal horn actually is.

    The iron that is left is structural. A wide band at the corner where the curl loads
    the shaft, a collar at the mouthpiece, and the ring a strap goes through - ironwork
    holding a horn together rather than ironwork the horn is made of. The ring earns its
    place twice over: it is also the only detail in the set that puts real daylight
    inside the outline, which is the trick coil plays with its whole body, bought here
    for one torus.
    """
    at, head = Vector((0.0, 0.0, 0.0)), 0.0

    # The rim, before the bell it belongs to, because the bell starts at the origin and
    # the rim sits on that first ring. Bone and 6mm proud: a horn's lip is the heaviest
    # part of it, and the version with an iron band here is the one that read as brass.
    band("crook_rim", Vector((0.0, 0.0, 0.0)), 0.0, 0.096, length=0.018, bend=3.0,
         mat="bone")

    # The bell. A straight cone over 10cm, 17cm across at the mouth - wide enough that
    # nothing about it can be a pipe bowl, and even enough that nothing about it is a
    # calculated flare.
    at, head = run("crook_flare", "bone", at, head, 0.09, 20.0, 0.090, 0.062,
                   rings=9, cap=False)

    # The curl. A hundred degrees, so the bell opens outward and a little up rather than
    # straight up out of a cup, and the outline stays open instead of closing into a claw.
    at, head = run("crook_curl", "bone", at, head, 0.20, 82.0, 0.062, 0.044)

    # The band at the corner, wide and heavy. This is where the weight of the bell hangs
    # off the shaft, so it is the one place iron belongs without being asked.
    band("crook_knuckle", at, head, 0.049, length=0.030, bend=6.0)

    # The shaft. Four degrees over 14cm, which is straight to look at and not straight to
    # build - a run at zero is clamped to one degree anyway, and a trace of curve sits in
    # the hand where a ruler does not.
    at, head = run("crook_shaft", "bone", at, head, 0.15, 5.0, 0.042, 0.030)

    # The strap ring, on the outside of the shaft's own curve so it stands clear of it.
    # Offset along the perpendicular by less than its own radius, so it bites into the
    # shaft rather than resting against it.
    side = Vector((math.cos(math.radians(head)), 0.0, -math.sin(math.radians(head))))
    hang = at + side * 0.028
    ring(0.032, 0.008, (hang.x, hang.y, hang.z), "iron", major=15, minor=5, rot_x=90.0)

    at, head = run("crook_lower", "bone", at, head, 0.16, 5.0, 0.030, 0.020)
    band("crook_collar", at, head, 0.023, length=0.024)

    run("crook_mouth", "iron", at, head, 0.06, 8.0, 0.019, 0.014, sides=11, cap=True)


DESIGNS = [
    ("bronze", bronze),
    ("ox", ox),
    ("coil", coil),
    ("stave", stave),
    ("wolf", wolf),
    ("scroll", scroll),
    ("crook", crook),
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
    paint()
    stage_scene(sun=1.4)

    # stage_scene lights for timber, and these horns are pale ivory. At the shared world
    # strength of 0.65 a cream object on a lit ground is one value throughout and no
    # silhouette can be judged, which is the failure the notes describe as dark brown
    # rendering as pale beige. Dimmed here rather than in vhbuild, because every other
    # mod's props are darker than this and want the light they have.
    bpy.context.scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.28

    # And a key on the camera's side of it. stage_scene's sun is aimed from +y, which is
    # behind the object from here, so every candidate was photographed against its own
    # shadow: a cream horn came out mid grey and the two materials were one value. The
    # angles are the icon pass's, which are already proven on these shapes.
    bpy.ops.object.light_add(type="SUN", location=(-0.7, -1.1, 0.8))
    key = bpy.context.active_object
    key.data.energy = 2.4
    key.rotation_euler = (math.radians(56.0), 0.0, math.radians(-34.0))

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = [min(c[i] for c in corners) for i in range(3)]
    hi = [max(c[i] for c in corners) for i in range(3)]
    centre = [(lo[i] + hi[i]) * 0.5 for i in range(3)]

    # Sat on the ground rather than floating, so the eye has somewhere to put it.
    obj.location.z -= lo[2]
    centre[2] -= lo[2]

    # The block stands clear of whatever is being staged rather than at a fixed 42cm,
    # which was measured off wolf and is inside scroll: the camera sits out on +x, so a
    # fixed block ends up in front of any candidate wider than the one it was set for,
    # and the reference hides the thing it is there to measure.
    reference_cube((hi[0] + 0.20, -0.06, 0.125))
    bpy.context.active_object.scale = (0.25, 0.25, 0.25)

    # A fifth further back than the hand distance wolf was framed at. The block now stands
    # off the candidate rather than at a fixed 42cm, so on a wide one it was walking out of
    # frame and taking the only thing that says how big any of this is with it.
    camera((0.74, -1.13, 0.62), (centre[0], centre[1], centre[2]), lens=50)


def paint():
    """
    Colours the two materials for a render.

    Only the renders need this. The world model borrows vanilla materials at runtime through
    Skins, so its colour comes from the game and nothing here reaches it - but an icon is a
    picture, and a picture has to carry its own paint. The first one shipped without any: both
    groups came out at Blender's default grey, so the inventory showed a grey horn while the
    photograph it was modelled from is cream and near-black. vhbuild's TINTS table has no "bone"
    entry, and nothing called tint() either, so it was grey twice over.

    Set here rather than in the shared TINTS table because these two values are this mod's
    reading of one photograph, not a palette anything else should inherit - and "iron" here is
    doing double duty as polished dark horn, which is much darker than iron ought to be.
    """
    colours = {
        # Pale ivory. The horn's body is the lightest thing in the inventory grid, which is
        # most of what makes it findable next to coins and a key.
        "bone": (0.87, 0.83, 0.72, 1.0),

        # Near-black, not grey. The mouthpiece and the medallion are polished horn in the
        # photograph, and the light-to-dark split along the length is the silhouette's only
        # internal landmark at 48 pixels.
        "iron": (0.07, 0.07, 0.08, 1.0),
    }

    for mat in bpy.data.materials:
        key = mat.name.split(".")[0].lower()
        if key not in colours:
            continue

        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if not bsdf:
            continue

        bsdf.inputs["Base Color"].default_value = colours[key]

        # Horn is polished but not a mirror. Left at Blender's default it reads as plastic.
        bsdf.inputs["Roughness"].default_value = 0.42


def icon_scene(obj, turn=180.0):
    """
    Orthographic, three quarters on, transparent, fitted.

    Three quarters rather than front on, which the sapling icon already paid for: dead
    front on flattens a small object into a symmetrical blob, and the whole job of an
    icon is an outline you can tell from its neighbours in a grid.

    Suns, not area lights. Area lights a metre from a 20cm object blow every channel to
    white, and the tell is dark brown rendering as pale beige.
    """
    paint()

    # Turned to match the photograph: bell up and to the right, dark mouthpiece down and to
    # the left. The sweep is built from the bell at the origin curving away toward +X, which
    # photographs the other way round - correct object, upside-down portrait.
    #
    # Done to the object for the render rather than to the mesh, because the mesh's own
    # orientation is what the held and dropped horn use and that is a separate question with
    # its own rule: item prefabs lie face-up, not standing.
    #
    # Half a turn is wolf's portrait and not a universal one, which is why it is an argument
    # now. twin is built standing on its mouthpiece block, so the same 180 hangs it from the
    # ceiling by its bells - and an upside-down object does not read as upside down, it reads
    # as a different object. A bipod, in that case.
    obj.rotation_euler = (0.0, math.radians(turn), 0.0)
    bpy.context.view_layer.update()

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
    icon_scene(obj, turn=ICON_TURN.get(label, 180.0))

    # 128, not 64. Valheim scales icons down and a sharp source survives that better
    # than one rendered at the size it will be shown at.
    render(os.path.join(PREVIEWS, name + "_icon.png"), width=128, height=128,
           bloom=False)
    render(os.path.join(PREVIEWS, name + "_large.png"), width=512, height=512,
           bloom=False)

    if winner:
        render(os.path.join(SHIPPED, SHIPPED_ICON), width=128, height=128, bloom=False)

    print("DESIGN_OK %s tris=%d%s" % (name, tris, " [SHIPPED]" if winner else ""))


def wanted():
    r"""
    Which candidates this run builds. Everything, unless told otherwise:

        blender --background --python tools/lur_designs.py -- only=scroll,fold

    A full run re-exports the winner over assets\lur.obj and repaints assets\lur.png, which
    is exactly right when the shipped horn is the thing being worked on and exactly wrong when
    it is not. Rendering a new candidate should not be able to touch the horn already in the
    game, so a subset run skips the winner and therefore skips that write entirely.
    """
    for arg in sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []:
        if arg.startswith("only="):
            picked = [name.strip() for name in arg[5:].split(",") if name.strip()]
            return [(label, maker) for label, maker in DESIGNS if label in picked]

    return DESIGNS


def main():
    os.makedirs(PREVIEWS, exist_ok=True)

    for label, maker in wanted():
        build(label, maker)


main()
