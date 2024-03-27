import datetime
import itertools
import logging
import pathlib
import random
import sys
import uuid

import pygame
import pygame_widgets
import redis
from pygame_widgets.slider import Slider

class AttrDict(dict):
    def __getattr__(self, key):
        return self[key]

    def __setattr__(self, key, value):
        self[key] = value

c = AttrDict()
c.REDIS_CONN_URL = "redis://10.0.1.36:6379"
c.REDIS_USER_GAVE_INPUT_KEY = "user_gave_input"
c.REDIS_STIMULUS_DONE_KEY = "stimulus_done"
c.WAIT_FOR_USER_INPUT = True
c.WAIT_FOR_STIMULUS_DONE = True
c.REDIS_STIMULUS_DONE_KEY = "stimulus_done"
c.REDIS_USER_GAVE_INPUT_KEY = "user_gave_input"
c.REDIS_CURRENT_SELECTED_KEY = "input_curr_selected"
c.REDIS_PLEASANTNESS_KEY = "pleasantness"
c.REDIS_INTENSITY_KEY = "intensity"
c.REDIS_SLEEPING_KEY = "sleeping"


r: redis.Redis = redis.Redis.from_url(c.REDIS_CONN_URL)


DEFAULT_OUTPUT_PATH = pathlib.Path(__file__).resolve().parent / "exp_results.csv"

logger = logging.getLogger(__file__)
hdlr = logging.FileHandler(__file__ + ".log")
formatter = logging.Formatter("%(asctime)s %(levelname)s %(message)s")
hdlr.setFormatter(formatter)
logger.addHandler(hdlr)
logger.setLevel(logging.DEBUG)


class ButtonLatch:
    def __init__(self):
        self.is_set = False
        self.rising = False

    def update(self, value):
        if value:
            if not self.is_set:
                self.is_set = True
                self.rising = True
            else:
                self.rising = False

        else:
            self.is_set = False
            self.rising = False


OK_BUTTONS = [0, 1, 2, 3, 14, 15, 16, 17]
OK_LATCHES = [ButtonLatch() for _ in OK_BUTTONS]

AXS_UP_TO_DOWN = [1, 3]
AXS_UP_TO_DOWN_LATCHES = [ButtonLatch() for _ in AXS_UP_TO_DOWN]
AXS_LEFT_TO_RIGHT = [0, 2]

SLIDER_MAX = 999
SLIDER_MIN = 0
SLIDER_STEP_COEFF = 6


locales = {
    "en": {
        "pleasantness": "How pleasant is the stroke ?",
        "arousal": "How intense is the stroke ?",
        "next": "Next",
        "locale": "Language: EN",
        "trial_n": "Trial n°:",
        "in_progress": "Stimulus in progress",
        "input_go": "Select your answers",
        "waiting_for_stimulus": "Waiting for stimulus",
        "very_pleasant": "Pleasant",
        "not_pleasant": "Not pleasant",
        "very_intense": "Intense",
        "not_intense": "Not intense",
    },
    "fr": {
        "pleasantness": "A quel point la caresse est-elle plaisante ?",
        "arousal": "A quel point la caresse est-elle intense ?",
        "next": "Suivant",
        "locale": "Langue: FR",
        "trial_n": "Essai n°:",
        "in_progress": "Stimulus en cours",
        "input_go": "Sélectionnez vos réponses",
        "waiting_for_stimulus": "En attente du stimulus",
        "very_pleasant": "Plaisant",
        "not_pleasant": "Non plaisant",
        "very_intense": "Intense",
        "not_intense": "Non intense",
    },
}

locales_wheel = itertools.cycle(locales.keys())


def set_user_input_done():
    r.set(c.REDIS_USER_GAVE_INPUT_KEY, "true")


def set_user_input_not_done():
    r.set(c.REDIS_USER_GAVE_INPUT_KEY, "false")


def is_stimulating():
    if c.REDIS_STIMULUS_DONE_KEY:
        if r.get(c.REDIS_STIMULUS_DONE_KEY) == b"true":
            return False

    return True


def user_has_given_input():
    if c.WAIT_FOR_USER_INPUT:
        return r.get(c.REDIS_USER_GAVE_INPUT_KEY) == b"true"

    return True


def save(path, exp_id, trial_n, pleas, arous):
    with open(path, "a", encoding="utf-8") as f:
        f.write(f"{exp_id},{trial_n},{pleas},{arous}\n")


def between(lower, upper, value):
    lower_bound = max(lower, value)
    return min(upper, lower_bound)


def reset_slider(slider: Slider):
    # random
    slider.setValue(random.randint(SLIDER_MIN, SLIDER_MAX))
    # middle
    # slider.setValue((SLIDER_MAX + SLIDER_MIN) / 2)


def is_reset():
    if c.WAIT_FOR_STIMULUS_DONE:
        if r.get(c.REDIS_STIMULUS_DONE_KEY) != b"true":
            return False

    return True


def main(output_path: pathlib.Path) -> int:
    LOCALE = next(locales_wheel)
    experiment_id = uuid.uuid4()
    trial_n = 1

    if not output_path.exists():
        if not output_path.parent.exists() and not output_path.parent.is_dir():
            output_path.parent.mkdir(parents=True)
        output_path.touch()
        with open(output_path, "w", encoding="utf-8") as f:
            f.write(f"experiment_id,trial_n,pleasantness,arousal\n")

    # This dict can be left as-is, since pygame will generate a
    # pygame.JOYDEVICEADDED event for every joystick connected
    # at the start of the program.
    joysticks = {}

    pygame.init()
    pygame.joystick.init()
    win = pygame.display.set_mode((1000, 800))

    clock = pygame.time.Clock()

    logger.info("pygame.joystick.get_count: %s", pygame.joystick.get_count())
    logger.info("pygame.joystick.get_init: %s", pygame.joystick.get_init())

    BLACK = (0, 0, 0)
    GREEN = (0, 255, 0)
    GRAY = (200, 200, 200)
    font = pygame.font.SysFont("didot.ttc", 48)
    font_small = pygame.font.SysFont("didot.ttc", 32)

    slider1 = Slider(win, 100, 280, 800, 40, min=SLIDER_MIN, max=SLIDER_MAX, step=1)
    slider2 = Slider(win, 100, 550, 800, 40, min=SLIDER_MIN, max=SLIDER_MAX, step=1)

    curr_selected = 0
    sliders = [slider1, slider2]
    for slider in sliders:
        reset_slider(slider)

    reset = False
    slider_moved = [False, False]

    since_last_reset = datetime.datetime.now()
    set_user_input_done()

    while True:
        events = pygame.event.get()
        for event in events:
            if event.type == pygame.QUIT:
                return 0

            if event.type == pygame.KEYDOWN:
                if event.key == pygame.K_UP:
                    curr_selected = max(0, curr_selected - 1)
                if event.key == pygame.K_DOWN:
                    curr_selected = min(3, curr_selected + 1)
                if event.key == pygame.K_RETURN:
                    if curr_selected == 2:
                        save(
                            output_path,
                            experiment_id,
                            trial_n,
                            slider1.getValue(),
                            slider2.getValue(),
                        )
                        reset = is_reset()
                    elif curr_selected == 3:
                        LOCALE = next(locales_wheel)
            if event.type == pygame.MOUSEBUTTONDOWN:
                print(event.button)
                if event.button == pygame.BUTTON_RIGHT:
                    save(
                        output_path,
                        experiment_id,
                        trial_n,
                        slider1.getValue(),
                        slider2.getValue(),
                    )
                    reset = is_reset()

            # Handle hotplugging
            if event.type == pygame.JOYDEVICEADDED:
                # This event will be generated when the program starts for every
                # joystick, filling up the list without needing to create them manually.
                joy = pygame.joystick.Joystick(event.device_index)
                joysticks[joy.get_instance_id()] = joy
                logger.info("Joystick %s connected", joy.get_instance_id())

            if event.type == pygame.JOYDEVICEREMOVED:
                del joysticks[event.instance_id]
                logger.info("Joystick %s disconnected", event.instance_id)

        ok_button_pressed = False
        for joystick in joysticks.values():
            for but_i, but in enumerate(OK_BUTTONS):
                but_val = joystick.get_button(but)
                OK_LATCHES[but_i].update(but_val)
                if OK_LATCHES[but_i].rising:
                    ok_button_pressed = True

            for ax, latch in zip(AXS_UP_TO_DOWN, AXS_UP_TO_DOWN_LATCHES):
                ax_val = joystick.get_axis(ax)
                latch.update(ax_val > 0.5 or ax_val < -0.5)
                if latch.rising and ax_val < -0.5:
                    curr_selected = max(0, curr_selected - 1)
                if latch.rising and ax_val > 0.5:
                    curr_selected = min(3, curr_selected + 1)

            for ax in AXS_LEFT_TO_RIGHT:
                ax_val = joystick.get_axis(ax)
                if abs(ax_val) < 0.3:
                    continue
                if curr_selected in [0, 1]:
                    new_val = (
                        sliders[curr_selected].getValue() + ax_val * SLIDER_STEP_COEFF
                    )
                    sliders[curr_selected].setValue(
                        between(SLIDER_MIN, SLIDER_MAX, new_val)
                    )

                    slider_moved[curr_selected] = True

        r.set(c.REDIS_CURRENT_SELECTED_KEY, str(curr_selected))

        if ok_button_pressed:
            if curr_selected == 2:
                reset = is_reset()
                if reset:
                    save(
                        output_path,
                        experiment_id,
                        trial_n,
                        slider1.getValue(),
                        slider2.getValue(),
                    )
            elif curr_selected == 3:
                LOCALE = next(locales_wheel)

        if reset:
            for slider in sliders:
                reset_slider(slider)
                curr_selected = 0
            reset = False
            # implies redis is running
            if c.WAIT_FOR_USER_INPUT:
                set_user_input_done()
            slider_moved = [False, False]
            trial_n += 1
            since_last_reset = datetime.datetime.now()

        trial_n_img = font.render(
            f"{locales[LOCALE]['trial_n']} {trial_n:>2}", True, BLACK
        )
        pleas_img = font.render(locales[LOCALE]["pleasantness"], True, BLACK)
        arous_img = font.render(locales[LOCALE]["arousal"], True, BLACK)
        next_img = font.render(locales[LOCALE]["next"], True, BLACK)
        next_img_green = font.render(locales[LOCALE]["next"], True, GREEN)
        locale_img = font.render(locales[LOCALE]["locale"], True, BLACK)
        locale_img_green = font.render(locales[LOCALE]["locale"], True, GREEN)
        in_progress_img = font.render(locales[LOCALE]["in_progress"], True, BLACK)
        input_go_img = font.render(locales[LOCALE]["input_go"], True, BLACK)
        one_img = font.render("1", True, BLACK)
        two_img = font.render("2", True, BLACK)
        three_img = font.render("3", True, BLACK)
        waiting_for_stimulus_img = font.render(
            locales[LOCALE]["waiting_for_stimulus"], True, BLACK
        )

        very_pleas_img = font_small.render(
            locales[LOCALE]["very_pleasant"], True, BLACK
        )
        very_arous_img = font_small.render(locales[LOCALE]["very_intense"], True, BLACK)
        not_pleas_img = font_small.render(locales[LOCALE]["not_pleasant"], True, BLACK)
        not_arous_img = font_small.render(locales[LOCALE]["not_intense"], True, BLACK)
        tick = font_small.render("|", True, BLACK)
        zero = font_small.render("0", True, BLACK)
        fifty = font_small.render("50", True, BLACK)
        hundred = font_small.render("100", True, BLACK)

        keys = pygame.key.get_pressed()
        if curr_selected < 2 and keys[pygame.K_RIGHT]:
            slider_moved[curr_selected] = True

            new_val = sliders[curr_selected].getValue() + SLIDER_STEP_COEFF
            sliders[curr_selected].setValue(between(SLIDER_MIN, SLIDER_MAX, new_val))
        if curr_selected < 2 and keys[pygame.K_LEFT]:
            slider_moved[curr_selected] = True

            new_val = sliders[curr_selected].getValue() - SLIDER_STEP_COEFF
            sliders[curr_selected].setValue(between(SLIDER_MIN, SLIDER_MAX, new_val))

        r.set(c.REDIS_PLEASANTNESS_KEY, str(sliders[0].getValue()))
        r.set(c.REDIS_INTENSITY_KEY, str(sliders[1].getValue()))


        win.fill((255, 255, 255))
        win.blit(trial_n_img, (100, 50))
        win.blit(pleas_img, (100, 180))
        win.blit(arous_img, (100, 450))

        # labels
        win.blit(not_pleas_img, (50, 250))
        win.blit(not_arous_img, (50, 520))
        win.blit(very_pleas_img, (850, 250))
        win.blit(very_arous_img, (850, 520))

        # ticks - pleasantness
        win.blit(tick, (97, 320))
        win.blit(tick, (497, 320))
        win.blit(tick, (898, 320))
        # ticks - intensity
        win.blit(tick, (97, 590))
        win.blit(tick, (497, 590))
        win.blit(tick, (898, 590))

        # numbers - pleasantness
        win.blit(zero, (94, 350))
        win.blit(fifty, (490, 350))
        win.blit(hundred, (884, 350))
        # numbers - intensity
        win.blit(zero, (94, 620))
        win.blit(fifty, (490, 620))
        win.blit(hundred, (884, 620))

        r_sleeping = r.get(c.REDIS_SLEEPING_KEY)
        sleeping = not r_sleeping == b"false"
        if is_stimulating():
            win.blit(in_progress_img, (550, 50))
        elif sleeping:
            sleeping_time_str = r.get(f"{c.REDIS_SLEEPING_KEY}:time").decode("utf-8")
            sleeping_time = datetime.datetime.fromisoformat(sleeping_time_str)
            sleeping_number = int(r_sleeping)

            to_show = sleeping_number - int(
                (datetime.datetime.now() - sleeping_time).total_seconds()
            )

            number_img = font.render(str(to_show), True, BLACK)
            win.blit(number_img, (550, 50))
        elif user_has_given_input():
            win.blit(waiting_for_stimulus_img, (550, 50))
        else:
            win.blit(input_go_img, (550, 50))

        for i, slider in enumerate(sliders):
            if i == curr_selected:
                slider.colour = GREEN
            else:
                slider.colour = GRAY

        if all(slider_moved):
            if curr_selected == 2:
                win.blit(next_img_green, (800, 700))
            else:
                win.blit(next_img, (800, 700))
        if curr_selected == 3:
            win.blit(locale_img_green, (100, 700))
        else:
            win.blit(locale_img, (100, 700))

        # slider1.setValue(random.randint(0, 99))
        # slider2.setValue(random.randint(0, 99))

        # output1.setText(slider1.getValue())
        # output2.setText(slider2.getValue())

        pygame_widgets.update(events)
        pygame.display.update()
        clock.tick(60)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        output_path = DEFAULT_OUTPUT_PATH
    else:
        output_path = pathlib.Path(sys.argv[1]).resolve()

    try:
        main(output_path)
    except Exception as e:
        logger.error("Error: %s\n", e, exc_info=1)
        raise e
    finally:
        pygame.quit()
