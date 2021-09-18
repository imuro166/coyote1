using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public List<int> cards; // 山札
    public List<int> holds; // 保留(コヨーテがされたら捨て札に)
    public List<int> discards; // 捨て札
    int[] numList; // 各カードの枚数の配列

    //
    public int GetCards()
    {
        int x;
        if (cards.Count == 1)
        {
            disShuffle();
        }
        if (cards.Count == 0)
        {
            x = discards[0];
            discards.RemoveAt(0); // discardsの最初の要素を削除
            return x;
        }
        x = cards[0];
        cards.RemoveAt(0); // cardsの最初の要素を削除
        holds.Add(x);   // holdsに取り出した要素を追加
        return x;
    }

    public void Create()
    {
        // cards, holds, discardsの初期化
        if (cards == null)
        {
            cards = new List<int>();
        }
        else
        {
            cards.Clear();
        }
        if (holds == null)
        {
            holds = new List<int>();
        }
        else
        {
            holds.Clear();
        }
        if (discards == null)
        {
            discards = new List<int>();
        }
        else
        {
            discards.Clear();
        }

        numList = new int[15] {
            3, 4, 4, 4, 4, 4, // 0～5
            3, 2, 1, 2, 1,    // 10, 15, 20, -5, -10 
            1, 1, 1, 1        // 特殊(0, *2, MAX0, ?) 
        };

        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < numList[i]; j++)
            {
                cards.Add(i);　//リストにiを加える。
            }
        }
    }

    // cardsをシャッフルする。
    public void Shuffle()
    {
        int n = cards.Count; //整数ｎの初期値はカードの枚数とする。
        while (n > 1)　//nが１より大きい場合下記を繰り返す。
        {
            n--;　//nをディクリメント
            int k = Random.Range(0, n + 1);//kは０～n+1の間のランダムな数とする
            int temp = cards[k];　//k番目のカードをtempに代入
            cards[k] = cards[n];　//k番目のインデックスにn番目のインデックスを代入
            cards[n] = temp;　//n番目のインデックスにtemp(k番目のインデックス）を代入
        }
    }

    // discardsをシャッフルする。(cards不足時用)
    public void disShuffle()
    {
        int n = discards.Count; //整数ｎの初期値はカードの枚数とする。
        while (n > 1)　//nが１より大きい場合下記を繰り返す。
        {
            n--;　//nをディクリメント
            int k = Random.Range(0, n + 1);//kは０～n+1の間のランダムな数とする
            int temp = discards[k];　//k番目のカードをtempに代入
            discards[k] = discards[n];　//k番目のインデックスにn番目のインデックスを代入
            discards[n] = temp;　//n番目のインデックスにtemp(k番目のインデックス）を代入
        }
    }

    public void ToDiscards()
    {
        // 0(特殊)がholdsにある場合は山札リセット。
        if (holds.Contains(11))
        {
            Create();
            Shuffle();
            return;
        }
        foreach (int n in holds)
        {
            discards.Add(n);
        }
        holds.Clear();
    }

    void Start()
    {
        Create();
        Shuffle();
    }
}
