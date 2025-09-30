import time

import webview

def wordListLogic(wordsfile, word, window):
    time.sleep(5)
    done = False
    while not done:
        word = word.rstrip('\n')
        print(word)
        if word == "":
            done = True
        else:
            url = 'https://en.wiktionary.org/wiki/' + word
            print(url)
            window.load_url(url)
            print(window.get_current_url())
            window.title = word
            word = wordsfile.readline()
        time.sleep(2)



window = webview.create_window('Manual Word Verification', url='https://en.wiktionary.org/wiki/crow')
wordsfile = open('words.txt', 'r')
word = wordsfile.readline()
webview.start(wordListLogic, [wordsfile, word, window])
print("hello")