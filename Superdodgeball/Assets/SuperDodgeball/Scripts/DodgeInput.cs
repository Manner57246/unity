using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball
{
    [Flags] public enum DodgeButtons { Aim = 1, Left = 2, Right = 4, L = 8, O = 16, Fire = 32, Dive = 64, Standard = 128, Special = 256, Outfit = 512, UseItem = 1024, Detonate = 2048, Collect = 4096, Drop = 8192 }
    public struct DodgeInput
    {
        public Vector2 move;
        public float yaw, pitch;
        public Vector3 aim;
        public DodgeButtons held, pressed;
        public bool Has(DodgeButtons b) => (held & b) != 0;
        public bool Down(DodgeButtons b) => (pressed & b) != 0;
        public static DodgeInput Read(DodgeCamera view)
        {
            var k = Keyboard.current; var m = Mouse.current;
            var input = new DodgeInput { yaw = view.Yaw, pitch = view.Pitch, aim = view.AimPoint };
            if (k == null || m == null || !DodgeGame.InputCaptured) return input;
            input.move = Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0)), 1f);
            if (m.rightButton.isPressed) input.held |= DodgeButtons.Aim;
            if (k.qKey.isPressed) input.held |= DodgeButtons.Left;
            if (k.eKey.isPressed) input.held |= DodgeButtons.Right;
            if (k.zKey.isPressed) input.held |= DodgeButtons.L;
            if (k.xKey.isPressed) input.held |= DodgeButtons.O;
            if (m.leftButton.wasPressedThisFrame) input.pressed |= DodgeButtons.Fire;
            if (k.spaceKey.wasPressedThisFrame) input.pressed |= DodgeButtons.Dive;
            if (k.digit1Key.wasPressedThisFrame) input.pressed |= DodgeButtons.Standard;
            if (k.digit2Key.wasPressedThisFrame) input.pressed |= DodgeButtons.Special;
            if (k.fKey.wasPressedThisFrame) input.pressed |= DodgeButtons.UseItem;
            if (k.rKey.wasPressedThisFrame) input.pressed |= DodgeButtons.Detonate;
            if (k.vKey.wasPressedThisFrame) input.pressed |= DodgeButtons.Drop;
            if (k.gKey.wasPressedThisFrame) input.pressed |= DodgeButtons.Collect;
            if (k.cKey.wasPressedThisFrame) input.pressed |= DodgeButtons.Outfit;
            return input;
        }
    }
}
