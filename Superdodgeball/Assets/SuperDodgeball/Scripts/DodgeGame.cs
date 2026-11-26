using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(AudioSource))]
    public sealed class DodgeGame : MonoBehaviour
    {
        public static DodgeGame Instance { get; private set; }
        public static bool InputCaptured => Cursor.lockState == CursorLockMode.Locked;
        public static bool SimulationActive => DodgeApp.Active ? DodgeApp.Instance.SimulationRunning : InputCaptured;
        public static bool IsReplica => DodgeApp.Active && DodgeApp.Instance.IsClient;
        public static bool MatchActive => !DodgeApp.Active || DodgeApp.Instance.MatchRunning;
        public static bool ShowHitboxes;
        public DodgePlayer player;
        public DodgeCamera view;
        public Transform launcher;
        public Transform muzzle;
        public Renderer launcherLamp;
        public Material ballMaterial;
        public Material trailMaterial;
        public Material popMaterial;
        public Material hitboxMaterial;
        public Material foamMaterial;
        public bool practiceLauncher;
        public bool endlessPractice = true;
        public bool airdropsEnabled = true;
        public bool botsEnabled = true;
        public bool crowdEnabled = true;
        public DodgeAirdrop ActiveDrop { get; private set; }
        float dropCountdown;
        int supplyNumber;
        DodgeCrowd supplyCrowd;
        public bool CapturedThisFrame { get; private set; }
        public int Score { get; private set; }
        public int Dodges { get; private set; }
        public int TimesHit { get; private set; }
        public int RoundNumber { get; private set; }
        public float SecondsLeft { get; private set; } = 90f;
        public bool RoundOver => !endlessPractice && SecondsLeft <= 0f;
        public bool RoundStarted { get; private set; }
        public float LauncherCountdown => Mathf.Max(0f, nextLaunch - Time.time);
        public string Notice { get; private set; } = "";
        public float NoticeUntil { get; private set; }
        float nextLaunch;
        Vector3 lockedShotPoint;
        bool shotLocked;
        AudioSource audioSource;
        LineRenderer warningLine;
        readonly List<GameObject> hitboxGhosts = new List<GameObject>();
        readonly Dictionary<GameObject, Collider> hitboxSources = new Dictionary<GameObject, Collider>();
        readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();

        void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            var lineObject = new GameObject("Launcher telegraph");
            lineObject.transform.SetParent(transform);
            warningLine = lineObject.AddComponent<LineRenderer>();
            warningLine.sharedMaterial = trailMaterial;
            warningLine.positionCount = 2;
            warningLine.startWidth = 0.025f;
            warningLine.endWidth = 0.07f;
            warningLine.startColor = new Color(1f, 0.3f, 0.15f, 0.6f);
            warningLine.endColor = new Color(1f, 0.65f, 0.1f, 0.8f);
            warningLine.enabled = false;
            ResetRound();
            UnlockInput();
        }

        void Update()
        {
            CapturedThisFrame = false;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (DodgeApp.Active) { UpdateWorld(); return; }
            if (keyboard == null || mouse == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) UnlockInput();
            else if (!InputCaptured && mouse.leftButton.wasPressedThisFrame && !RoundOver && Application.isFocused)
                CaptureInput();

            if (InputCaptured)
            {
                if (keyboard.rKey.wasPressedThisFrame) ResetRound();
                if (keyboard.tKey.wasPressedThisFrame) ToggleLauncher();
                if (keyboard.hKey.wasPressedThisFrame) ToggleHitboxes();
                if (keyboard.bKey.wasPressedThisFrame) ToggleBots();
                if (keyboard.vKey.wasPressedThisFrame) ToggleCrowd();
            }
            if (!endlessPractice && RoundStarted && InputCaptured && !RoundOver)
            {
                SecondsLeft = Mathf.Max(0f, SecondsLeft - Time.deltaTime);
                if (RoundOver)
                {
                    UnlockInput();
                    warningLine.enabled = false;
                    ClearBalls();
                    return;
                }
            }
            UpdateWorld();
        }

        void UpdateWorld()
        {
            if (IsReplica) return;
            if (!MatchActive) { warningLine.enabled=false;return; }
            if (airdropsEnabled && RoundStarted && SimulationActive && !RoundOver && !ActiveDrop)
            {
                if (DodgeApp.Active)
                {
                    var app=DodgeApp.Instance;
                    if (supplyNumber==0 && app.MatchRunning && app.TimeLeft<=app.Room.duration*.5f)
                    {
                        if(!supplyCrowd)supplyCrowd=FindFirstObjectByType<DodgeCrowd>();
                        if(supplyCrowd && !supplyCrowd.Pending)supplyCrowd.ScheduleThrow(player.GetComponent<DodgeActor>(),DodgeCrowdThrow.Zeus);
                    }
                }
                else if ((dropCountdown-=Time.deltaTime)<=0)
                    SpawnAirdrop(new Vector3(Random.Range(-4.5f,4.5f),0,Random.Range(-2f,3f)),DodgeArsenal.RandomSupply());
            }
            bool fireActive = practiceLauncher && RoundStarted && SimulationActive && !RoundOver;
            warningLine.enabled = fireActive && LauncherCountdown <= 0.65f;
            if (!fireActive)
            {
                nextLaunch = Time.time + 1.8f;
                shotLocked = false;
                return;
            }
            if (LauncherCountdown <= 0.65f && !shotLocked)
            {
                // Aim is locked during the warning; a successful pose can actually evade it.
                lockedShotPoint = player.transform.position + Vector3.up * 1.72f;
                shotLocked = true;
            }
            if (shotLocked)
            {
                warningLine.SetPosition(0, muzzle.position);
                warningLine.SetPosition(1, lockedShotPoint);
                Vector3 direction = lockedShotPoint - launcher.position;
                launcher.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
            }
            if (Time.time >= nextLaunch)
            {
                DodgeBall.Spawn(muzzle.position, DodgeBall.SolveLaunchVelocity(muzzle.position, lockedShotPoint, 15f),
                    null, true, ballMaterial);
                PlayTone(190f, 0.09f, 0.1f);
                nextLaunch = Time.time + 2.6f;
                shotLocked = false;
            }
        }

        public void CaptureInput()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RoundStarted = true;
            CapturedThisFrame = true;
            nextLaunch = Time.time + 2f;
        }

        public void UnlockInput()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) UnlockInput();
        }

        public void ToggleLauncher()
        {
            practiceLauncher = !practiceLauncher;
            nextLaunch = Time.time + 2f;
            shotLocked = false;
            SetNotice(practiceLauncher ? "发球机关开启 · 看见红线后准备侧弯" : "发球机关关闭 · 自由练习投球", 2.5f);
        }

        public void RegisterHit(DodgeActor actor, Vector3 point, bool playerShot = true)
        {
            if (RoundOver) return;
            if (actor.isPlayer)
            {
                TimesHit++;
                SetNotice("中弹了！试试在球接近时按 Q / E", 1.5f);
                view.Kick(0.045f);
                PlayTone(110f, 0.16f, 0.2f);
            }
            else if (playerShot)
            {
                Score += 10;
                SetNotice("命中！ +10", 0.9f);
                PlayTone(760f, 0.08f, 0.13f);
            }
        }

        public void RegisterDodge()
        {
            if (RoundOver) return;
            Dodges++;
            SetNotice("躲过去了！", 1f);
            PlayTone(920f, 0.07f, 0.1f);
        }

        public void SetNotice(string text, float duration)
        {
            Notice = text;
            NoticeUntil = Time.time + duration;
        }

        public void ResetRound(bool carryTraining = false)
        {
            if(DodgePartyEffects.Instance)DodgePartyEffects.Instance.ResetEffects();
            RoundNumber++;
            ClearAirdrop();
            foreach (var drop in new List<DodgeAirdrop>(DodgeAirdrop.Active))
                if (drop) { drop.gameObject.SetActive(false); Destroy(drop.gameObject); }
            foreach (var crowd in FindObjectsByType<DodgeCrowd>(FindObjectsSortMode.None)) crowd.ResetCrowd();
            dropCountdown = 3f; supplyNumber = 0;
            ClearBalls();
            Score = Dodges = TimesHit = 0;
            SecondsLeft = 90f;
            RoundStarted = false;
            shotLocked = false;
            nextLaunch = Time.time + 2f;
            player.ResetPlayer();
            foreach (var actor in FindObjectsByType<DodgeActor>(FindObjectsSortMode.None)) actor.ResetActor(carryTraining);
            foreach (var bot in FindObjectsByType<DodgeBot>(FindObjectsSortMode.None)) bot.ResetBrain();
            view.SetView(0f, 9f);
            Notice = "";
            NoticeUntil = 0f;
            if (InputCaptured) RoundStarted = true;
        }

        public void ClearBalls()
        {
            foreach (var ball in FindObjectsByType<DodgeBall>(FindObjectsSortMode.None))
            {
                ball.enabled = false;
                Destroy(ball.gameObject);
            }
        }

        public DodgeAirdrop SpawnAirdrop(Vector3 point, DodgeWeapon weapon, bool landed = false, DodgeItem item=DodgeItem.None)
        {
            ClearAirdrop();
            ActiveDrop = DodgeAirdrop.Spawn(this, point, weapon, landed,item==DodgeItem.None?DodgeGiftKind.Weapon:DodgeGiftKind.Item,item);
            dropCountdown = Random.Range(10f, 15f);
            PlayTone(600f, 0.16f, 0.09f);
            return ActiveDrop;
        }

        internal void RegisterThundercloud(DodgeAirdrop cloud)
        {
            ActiveDrop=cloud;supplyNumber++;
        }

        public void ClearAirdrop()
        {
            if (ActiveDrop)
            {
                ActiveDrop.gameObject.SetActive(false);
                Destroy(ActiveDrop.gameObject);
            }
            ActiveDrop = null;
        }

        public void ToggleBots()
        {
            botsEnabled = !botsEnabled;
            ResetRound();
        }

        public void ToggleCrowd()
        {
            crowdEnabled = !crowdEnabled;
            ResetRound();
        }

        public void ToggleHitboxes()
        {
            ShowHitboxes = !ShowHitboxes;
            if (hitboxGhosts.Count == 0)
            {
                foreach (var hitbox in FindObjectsByType<DodgeHitbox>(FindObjectsSortMode.None))
                {
                    var collider = hitbox.GetComponent<Collider>();
                    if (!collider || !collider.enabled) continue;
                    var ghost = GameObject.CreatePrimitive(collider is SphereCollider ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                    ghost.name = "Hitbox overlay";
                    ghost.transform.SetParent(hitbox.transform, false);
                    var ghostCollider = ghost.GetComponent<Collider>();
                    ghostCollider.enabled = false;
                    Destroy(ghostCollider);
                    ghost.GetComponent<Renderer>().sharedMaterial = hitboxMaterial;
                    hitboxGhosts.Add(ghost);
                    hitboxSources.Add(ghost, collider);
                }
            }
            foreach (var ghost in hitboxGhosts) if (ghost) ghost.SetActive(ShowHitboxes);
            SetNotice(ShowHitboxes ? "命中区域显示 · 绿色部分才会被球击中" : "命中区域隐藏", 2f);
        }

        void LateUpdate()
        {
            if (!ShowHitboxes) return;
            foreach (var pair in hitboxSources)
            {
                if (!pair.Key || !pair.Value) continue;
                var ghost = pair.Key.transform;
                if (pair.Value is SphereCollider sphere)
                {
                    ghost.localPosition = sphere.center;
                    ghost.localScale = Vector3.one * (sphere.radius * 2f * 1.02f);
                }
                else if (pair.Value is CapsuleCollider capsule)
                {
                    ghost.localPosition = capsule.center;
                    ghost.localRotation = capsule.direction == 0 ? Quaternion.Euler(0f, 0f, 90f)
                        : capsule.direction == 2 ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
                    ghost.localScale = new Vector3(capsule.radius * 2f, capsule.height * 0.5f, capsule.radius * 2f) * 1.02f;
                }
            }
        }

        public void SpawnPop(Vector3 point, Color color)
        {
            for (int i = 0; i < 7; i++)
            {
                var bit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bit.name = "Impact confetti";
                bit.transform.position = point;
                bit.transform.rotation = Random.rotation;
                bit.transform.localScale = Vector3.one * Random.Range(0.07f, 0.13f);
                var collider = bit.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                var renderer = bit.GetComponent<Renderer>();
                renderer.sharedMaterial = popMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
                bit.AddComponent<DodgePop>().velocity = Random.onUnitSphere * Random.Range(1.2f, 2.5f) + Vector3.up;
            }
        }

        public void EmitFoam(Vector3 position, Vector3 drift, int count = 3, float strength = 1f)
        {
            DodgePuff.Emit(position, drift, foamMaterial ? foamMaterial : popMaterial, count, strength);
        }

        public void PlayTone(float frequency, float duration, float volume)
        {
            int key = Mathf.RoundToInt(frequency / 25f) * 25;
            frequency = key;
            if (!tones.TryGetValue(key, out var clip))
            {
                int length = Mathf.CeilToInt(duration * 22050);
                var samples = new float[length];
                for (int i = 0; i < length; i++)
                {
                    float t = i / 22050f;
                    float envelope = Mathf.Sin(Mathf.PI * i / length) * Mathf.Exp(-t * 14f);
                    samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * 0.4f;
                }
                clip = AudioClip.Create("Dodgeball tone " + key, length, 1, 22050, false);
                clip.SetData(samples, 0);
                tones.Add(key, clip);
            }
            audioSource.PlayOneShot(clip, volume);
        }

        void OnDestroy()
        {
            foreach (var clip in tones.Values) if (clip) Destroy(clip);
            if (Instance == this) Instance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
