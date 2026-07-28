#!/usr/bin/env python3
"""
BunkerBloom 밸런싱 시뮬레이터
docs/balance-design-plan.md §7 사양 기반.

정책 4종을 30턴 시뮬해 생존일을 비교한다.
의존성 없음. 표준 라이브러리만 사용.

실행: python3 tools/balance_sim.py
"""
from dataclasses import dataclass, field
from typing import List, Optional, Tuple

# ── 상수 (계획서 §3, §4) ─────────────────────────────────────────────────

START_FOOD       = 50.0    # 압박 강화 (60→50)
START_POWER      = 80.0    # 압박 강화 (100→80)
START_SURVIVORS  = 3
PLOT_COUNT       = 12

PLANT_COST       = 5.0
HARVEST_COST     = 10.0    # 드론 수확 전기 비용

# 드론 오버히트 — 턴당 액션 한도 (Plant + Harvest 합산)
ACTION_CAP_PER_TURN = 3

# 맨몸 수확 — Power < HARVEST_COST일 때 폴백
MANUAL_HARVEST_INJURY_THRESHOLD = 2   # 같은 턴 2회 이상 → 생존자 -1

FOOD_PER_SURVIVOR  = 10.0
POWER_PER_SURVIVOR = 0.0
BUNKER_POWER       = 10.0  # 압박 강화 (8→10)

EVENT_BLACKOUT_DAY = 5
EVENT_FUNGAL_DAY   = 7

MAX_TURNS = 30

# ── 작물 ─────────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class CropDef:
    name: str
    grow_turns: int
    harvest_count: int
    food_per_unit: float
    power_per_unit: float

POTATO = CropDef("잿빛감자", 1, 4, 6.0, 1.0)   # grow_turns 1로 단축 (T1 prep → T2 수확)
VINE   = CropDef("기름덩굴", 1, 3, 1.0, 10.0)

# ── 상태 ─────────────────────────────────────────────────────────────────

@dataclass
class Plot:
    crop: Optional[CropDef] = None
    turns_left: int = 0

    def is_empty(self) -> bool:
        return self.crop is None

    def is_ripe(self) -> bool:
        return self.crop is not None and self.turns_left <= 0

    def plant(self, crop: CropDef) -> None:
        self.crop = crop
        self.turns_left = crop.grow_turns

    def advance(self) -> None:
        if self.crop is not None and self.turns_left > 0:
            self.turns_left -= 1

    def clear(self) -> Optional[CropDef]:
        c = self.crop
        self.crop = None
        self.turns_left = 0
        return c


@dataclass
class State:
    food: float = START_FOOD
    power: float = START_POWER
    survivors: int = START_SURVIVORS
    day: int = 1
    plots: List[Plot] = field(default_factory=lambda: [Plot() for _ in range(PLOT_COUNT)])
    inv_potato: int = 0
    inv_vine: int = 0
    fungal_this_turn: bool = False
    starvation_pending: bool = False
    actions_left: int = ACTION_CAP_PER_TURN   # 드론 오버히트 한도
    manual_harvests_this_turn: int = 0        # 맨몸 수확 카운터
    total_manual_harvests: int = 0            # 누적 (통계용)
    log: List[str] = field(default_factory=list)
    death_reason: Optional[str] = None

    def alive(self) -> bool:
        return self.death_reason is None

    def daily_food_need(self) -> float:
        return self.survivors * FOOD_PER_SURVIVOR

    def daily_power_need(self) -> float:
        return BUNKER_POWER + self.survivors * POWER_PER_SURVIVOR


# ── 정책 ─────────────────────────────────────────────────────────────────

@dataclass
class Policy:
    name: str
    potato_ratio: float            # 0.0~1.0; 빈 칸을 채울 때 감자 비율
    convert: str                   # "greedy" | "balanced" | "all_food" | "all_power"
    max_plants_per_turn: int = 4   # 사람이 한 턴에 심는 합리적 상한


POLICIES = [
    Policy("Greedy",       0.5, "greedy",      max_plants_per_turn=6),
    Policy("Balanced",     0.5, "balanced",    max_plants_per_turn=6),
    Policy("Fixed-Food",   1.0, "all_food",    max_plants_per_turn=6),
    Policy("Fixed-Power",  0.0, "all_power",   max_plants_per_turn=6),
]


# ── 시뮬 단계 ────────────────────────────────────────────────────────────

def step_reset_actions(state: State) -> None:
    state.actions_left = ACTION_CAP_PER_TURN
    state.manual_harvests_this_turn = 0


def step_event(state: State) -> None:
    state.fungal_this_turn = False
    if state.day == EVENT_BLACKOUT_DAY:
        state.power -= 20.0
        state.log.append(f"  ⚠ 정전 위협: P-20")
    if state.day == EVENT_FUNGAL_DAY:
        state.fungal_this_turn = True
        state.log.append(f"  ⚠ 곰팡이: 덩굴 ×2 (이번 턴)")


def step_plant(state: State, policy: Policy) -> None:
    if state.actions_left <= 0:
        return
    empty = [p for p in state.plots if p.is_empty()]
    if not empty:
        return
    # 이번 턴 유지비를 예약 (이미 액션 카운트된 harvest 비용은 차감됨)
    reserve = state.daily_power_need()
    available_power = max(0.0, state.power - reserve)
    affordable = int(available_power // PLANT_COST)
    n = min(len(empty), affordable, state.actions_left, policy.max_plants_per_turn)
    if n <= 0:
        return

    n_potato = int(round(n * policy.potato_ratio))
    n_vine   = n - n_potato

    for i, plot in enumerate(empty[:n]):
        plot.plant(POTATO if i < n_potato else VINE)
        state.power -= PLANT_COST
    state.actions_left -= n

    state.log.append(f"  심기: 감자 {n_potato}, 덩굴 {n_vine}  (-{n*PLANT_COST:.0f}P, 액션 -{n} → 잔여 {state.actions_left})")


def step_advance_growth(state: State) -> None:
    for p in state.plots:
        p.advance()


def step_harvest(state: State) -> None:
    if state.actions_left <= 0:
        return
    ripe = [p for p in state.plots if p.is_ripe()]
    if not ripe:
        return

    h_potato_units = h_vine_units = 0
    drone_count = manual_count = 0

    for plot in ripe:
        if state.actions_left <= 0:
            break
        # 드론 가능 시 드론 우선
        if state.power >= HARVEST_COST:
            state.power -= HARVEST_COST
            drone_count += 1
        else:
            # 맨몸 수확 폴백 (Power 0 비용, 부상 위험)
            state.manual_harvests_this_turn += 1
            state.total_manual_harvests   += 1
            manual_count += 1

        crop = plot.clear()
        state.actions_left -= 1
        if crop is POTATO:
            h_potato_units += POTATO.harvest_count
        else:
            mult = 2 if state.fungal_this_turn else 1
            h_vine_units += VINE.harvest_count * mult

    state.inv_potato += h_potato_units
    state.inv_vine   += h_vine_units

    parts = []
    if drone_count > 0:  parts.append(f"드론 {drone_count}회 -{drone_count*HARVEST_COST:.0f}P")
    if manual_count > 0: parts.append(f"⚠맨몸 {manual_count}회")
    cost_str = ", ".join(parts) if parts else "—"
    state.log.append(f"  수확: 감자 +{h_potato_units}, 덩굴 +{h_vine_units}  ({cost_str}, 액션 잔여 {state.actions_left})")
    if len(ripe) > drone_count + manual_count:
        unused = len(ripe) - drone_count - manual_count
        state.log.append(f"  ⚠ 익은 작물 {unused}개 — 액션/전력 부족으로 미수확")


def step_convert(state: State, policy: Policy) -> None:
    np_, nv_ = state.inv_potato, state.inv_vine
    if np_ == 0 and nv_ == 0:
        return

    food_gain = power_gain = 0.0

    if policy.convert == "all_food":
        food_gain = np_ * POTATO.food_per_unit + nv_ * VINE.food_per_unit
    elif policy.convert == "all_power":
        power_gain = np_ * POTATO.power_per_unit + nv_ * VINE.power_per_unit
    elif policy.convert == "balanced":
        np_food, np_power = np_ // 2, np_ - np_ // 2
        nv_food, nv_power = nv_ // 2, nv_ - nv_ // 2
        food_gain  = np_food  * POTATO.food_per_unit  + nv_food  * VINE.food_per_unit
        power_gain = np_power * POTATO.power_per_unit + nv_power * VINE.power_per_unit
    elif policy.convert == "greedy":
        # Smart routing: 작물 특성을 살림 — 감자→식량, 덩굴→전기
        # 단, 한쪽이 위태롭게 부족하면 모두 그쪽으로 몰빵
        food_critical  = state.food  < state.daily_food_need()
        power_critical = state.power < state.daily_power_need()
        if food_critical and not power_critical:
            food_gain = np_ * POTATO.food_per_unit + nv_ * VINE.food_per_unit
        elif power_critical and not food_critical:
            power_gain = np_ * POTATO.power_per_unit + nv_ * VINE.power_per_unit
        else:
            food_gain  = np_ * POTATO.food_per_unit
            power_gain = nv_ * VINE.power_per_unit

    state.food  += food_gain
    state.power += power_gain
    state.inv_potato = state.inv_vine = 0
    state.log.append(f"  변환({policy.convert}): F+{food_gain:.0f}, P+{power_gain:.0f}  (F={state.food:.0f}, P={state.power:.0f})")


def step_consume(state: State) -> None:
    f_use = state.daily_food_need()
    p_use = state.daily_power_need()
    state.food  -= f_use
    state.power -= p_use
    state.log.append(f"  소비: F-{f_use:.0f}, P-{p_use:.0f}  (F={state.food:.0f}, P={state.power:.0f})")


def step_check_death(state: State) -> None:
    # 1) 전기 0 이하 → 즉시 게임오버
    if state.power <= 0:
        state.death_reason = f"POWER_DEPLETED on M.{state.day:02d}"
        return
    # 2) 맨몸 수확 누적 부상 → 생존자 사망
    if state.manual_harvests_this_turn >= MANUAL_HARVEST_INJURY_THRESHOLD:
        state.survivors -= 1
        state.log.append(f"  ☠ 맨몸 수확 부상 (오염/부상): 생존자 {state.survivors}명")
        if state.survivors <= 0:
            state.death_reason = f"ALL_DEAD (manual exhaustion) on M.{state.day:02d}"
            return
    # 3) 식량 음수 1턴 누적 → 사망
    if state.food < 0:
        if state.starvation_pending:
            state.survivors -= 1
            state.starvation_pending = False
            state.log.append(f"  ☠ 기아 사망: 생존자 {state.survivors}명 남음")
            if state.survivors <= 0:
                state.death_reason = f"ALL_DEAD (starvation) on M.{state.day:02d}"
                return
            state.food = 0
        else:
            state.starvation_pending = True
            state.log.append(f"  ⚠ 식량 부족 (1턴 누적)")
    else:
        state.starvation_pending = False


# ── 메인 루프 ────────────────────────────────────────────────────────────

def simulate(policy: Policy) -> State:
    state = State()
    while state.day <= MAX_TURNS and state.alive():
        state.log.append(
            f"M.{state.day:02d} START | F={state.food:.0f} P={state.power:.0f} S={state.survivors} "
            f"| inv: 감자{state.inv_potato}/덩굴{state.inv_vine}"
        )
        # 새 순서: 액션 리셋 → 이벤트 → 성장(전 턴 작물 익음) → 수확 → 심기 → 변환 → 소비 → 사망체크
        step_reset_actions(state)
        step_event(state)
        step_advance_growth(state)
        step_harvest(state)
        step_plant(state, policy)
        step_convert(state, policy)
        step_consume(state)
        step_check_death(state)
        if not state.alive():
            state.log.append(f"  ✗ {state.death_reason}")
            break
        state.day += 1
    return state


# ── 출력 ─────────────────────────────────────────────────────────────────

def survived_days(s: State) -> int:
    return s.day - 1 if s.alive() else s.day


def print_summary(rows: List[Tuple[Policy, State]]) -> None:
    print("\n=== 결과 요약 ===\n")
    print(f"{'Policy':<14} | {'Days':>5} | {'F-end':>6} | {'P-end':>6} | {'Surv':>4} | Death")
    print("-" * 70)
    for p, s in rows:
        days = survived_days(s)
        reason = s.death_reason or "SURVIVED 30T"
        print(f"{p.name:<14} | {days:>5} | {s.food:>6.0f} | {s.power:>6.0f} | {s.survivors:>4} | {reason}")
    print()


def print_detail(rows: List[Tuple[Policy, State]]) -> None:
    for p, s in rows:
        print(f"\n=== {p.name} 상세 로그 ===")
        for line in s.log:
            print(line)


def check_pass(rows: List[Tuple[Policy, State]]) -> bool:
    """§9-A 검증 기준."""
    by = {p.name: s for p, s in rows}
    g  = survived_days(by["Greedy"])
    ff = survived_days(by["Fixed-Food"])
    fp = survived_days(by["Fixed-Power"])

    cond_g  = g  >= 10
    cond_ff = ff <= 5
    cond_fp = fp <= 5

    print("=== §9-A 검증 ===")
    print(f"Greedy ≥ 10턴       : {g}턴   → {'PASS' if cond_g  else 'FAIL'}")
    print(f"Fixed-Food ≤ 5턴 사망: {ff}턴 → {'PASS' if cond_ff else 'FAIL'}")
    print(f"Fixed-Power ≤ 5턴 사망: {fp}턴 → {'PASS' if cond_fp else 'FAIL'}")
    print()
    return cond_g and cond_ff and cond_fp


# ── 인터랙티브 모드 (재미 검증용) ────────────────────────────────────────

def _plot_status(state: State) -> str:
    e = sum(1 for p in state.plots if p.is_empty())
    gp = sum(1 for p in state.plots if p.crop is POTATO and not p.is_ripe())
    gv = sum(1 for p in state.plots if p.crop is VINE   and not p.is_ripe())
    rp = sum(1 for p in state.plots if p.is_ripe() and p.crop is POTATO)
    rv = sum(1 for p in state.plots if p.is_ripe() and p.crop is VINE)
    return f"빈{e} | 자라는중 감{gp}/덩{gv} | 익음 감{rp}/덩{rv}"


def _do_plant(state: State, crop_def, n: int) -> None:
    if n <= 0: return
    if state.actions_left < n: print(f"  ✗ 액션 부족 ({state.actions_left}/3)"); return
    if state.power < n * PLANT_COST: print(f"  ✗ 전기 부족 (-{n*PLANT_COST:.0f}P 필요, P={state.power:.0f})"); return
    empty = [p for p in state.plots if p.is_empty()]
    if len(empty) < n: print(f"  ✗ 빈 칸 부족 ({len(empty)})"); return
    for plot in empty[:n]: plot.plant(crop_def)
    state.power       -= n * PLANT_COST
    state.actions_left -= n
    print(f"  ✓ {crop_def.name} {n}개 심음 (-{n*PLANT_COST:.0f}P, 액션 {state.actions_left} 남음)")


def _do_harvest(state: State, n: int) -> None:
    if n <= 0: return
    if state.actions_left < n: print(f"  ✗ 액션 부족"); return
    ripe = [p for p in state.plots if p.is_ripe()]
    if len(ripe) < n: print(f"  ✗ 익은 작물 부족 ({len(ripe)})"); return
    drone = manual = 0; hp = hv = 0
    for plot in ripe[:n]:
        if state.power >= HARVEST_COST:
            state.power -= HARVEST_COST; drone += 1
        else:
            state.manual_harvests_this_turn += 1
            state.total_manual_harvests   += 1
            manual += 1
        crop = plot.clear()
        if crop is POTATO: hp += POTATO.harvest_count
        else:              hv += VINE.harvest_count * (2 if state.fungal_this_turn else 1)
    state.inv_potato += hp
    state.inv_vine   += hv
    state.actions_left -= n
    msg = f"  ✓ 수확: 감자 +{hp}, 덩굴 +{hv}"
    if drone:  msg += f"  드론{drone}"
    if manual: msg += f"  ⚠맨몸{manual}"
    print(msg + f"  (액션 {state.actions_left} 남음)")
    if state.manual_harvests_this_turn >= MANUAL_HARVEST_INJURY_THRESHOLD:
        print(f"  💀 맨몸 수확 {state.manual_harvests_this_turn}회 누적 — 턴 종료 시 부상자 발생!")


def _do_convert(state: State, food_pct: int) -> None:
    food_pct = max(0, min(100, food_pct))
    np_, nv_ = state.inv_potato, state.inv_vine
    if np_ == 0 and nv_ == 0:
        print("  변환할 인벤토리 없음"); return
    fp = int(np_ * food_pct / 100); pp = np_ - fp
    fv = int(nv_ * food_pct / 100); pv = nv_ - fv
    food_gain  = fp * POTATO.food_per_unit  + fv * VINE.food_per_unit
    power_gain = pp * POTATO.power_per_unit + pv * VINE.power_per_unit
    state.food  += food_gain
    state.power += power_gain
    state.inv_potato = state.inv_vine = 0
    print(f"  ✓ 변환({food_pct}% 식량): F+{food_gain:.0f}, P+{power_gain:.0f}")


def play_interactive() -> None:
    state = State()
    print("\n=== Bunker Bloom — 손맛 검증 ===")
    print("명령:  p N / v N / h N / c PCT / e (턴종료) / q (종료)")
    print("       p2  = 감자 2개 심기, v1 = 덩굴 1개, h3 = 3개 수확")
    print("       c60 = 인벤토리 60% 식량 변환 (나머지 전기)")

    fun_log = {"plant_or_harvest": 0, "convert": 0, "manual": 0}

    while state.day <= MAX_TURNS and state.alive():
        step_reset_actions(state)
        step_event(state)
        step_advance_growth(state)

        print(f"\n━━━━ M.{state.day:02d} ━━━━")
        print(f"Food {state.food:.0f}  |  Power {state.power:.0f}  |  생존자 {state.survivors}명")
        print(_plot_status(state))
        print(f"인벤토리: 감자 {state.inv_potato} / 덩굴 {state.inv_vine}")
        print(f"턴 종료 시 자동: F-{state.daily_food_need():.0f},  P-{state.daily_power_need():.0f}")

        ended = False
        while not ended:
            try:
                raw = input(f"[액션 {state.actions_left}/3] > ").strip()
            except EOFError:
                return
            if not raw: continue
            verb = raw[0].lower()
            arg  = raw[1:].strip()
            n    = int(arg) if arg.isdigit() else (1 if verb in "pvh" else 0)

            if   verb == 'q': print("종료"); return
            elif verb == 'e': ended = True
            elif verb == 'p': _do_plant(state, POTATO, n);  fun_log["plant_or_harvest"] += 1
            elif verb == 'v': _do_plant(state, VINE,   n);  fun_log["plant_or_harvest"] += 1
            elif verb == 'h':
                before_manual = state.manual_harvests_this_turn
                _do_harvest(state, n)
                if state.manual_harvests_this_turn > before_manual: fun_log["manual"] += 1
                fun_log["plant_or_harvest"] += 1
            elif verb == 'c':
                _do_convert(state, n)
                fun_log["convert"] += 1
            else:
                print("  ? 알 수 없는 명령")

        # 턴 종료: 미변환 인벤토리 자동 변환 안 함 → 인벤토리 캐리오버
        # (현실: 보관 손실은 일단 없음으로 단순화)
        step_consume(state)
        step_check_death(state)
        if not state.alive():
            print(f"\n💀 {state.death_reason}")
            break
        print(f"턴 종료: F={state.food:.0f}, P={state.power:.0f}, 생존자 {state.survivors}")
        state.day += 1

    print(f"\n━━━ 최종 ━━━")
    print(f"버틴 일수: {survived_days(state)}일")
    print(f"맨몸 수확 누적: {state.total_manual_harvests}회")
    print(f"\n재미 체크리스트:")
    print(f"  [{'✓' if fun_log['plant_or_harvest'] >= 6 else ' '}] 매 턴 액션 결정 6회+ ({fun_log['plant_or_harvest']})")
    print(f"  [{'✓' if fun_log['convert']         >= 3 else ' '}] 변환 결정 3회+ ({fun_log['convert']})")
    print(f"  [{'✓' if fun_log['manual']          >= 1 else ' '}] 맨몸 수확 도박 1회+ ({fun_log['manual']})")
    print(f"  [ ] 직접 평가: '심을까 수확할까' 고민이 매 턴 발생했는가? (Y/N)")


# ── 진입점 ───────────────────────────────────────────────────────────────

def main() -> None:
    import sys
    if len(sys.argv) > 1 and sys.argv[1] == "play":
        play_interactive()
        return

    rows = [(p, simulate(p)) for p in POLICIES]
    print_summary(rows)
    print_detail(rows)
    print()
    ok = check_pass(rows)
    print("=" * 50)
    print("결과: " + ("✓ 모두 통과" if ok else "✗ 수치 조정 필요"))


if __name__ == "__main__":
    main()
